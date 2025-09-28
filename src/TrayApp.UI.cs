using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TrayApp.Core;
using TrayApp.Configuration;

namespace TrayApp.UI
{
    /// <summary>
    /// 托盘图标管理器
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ILogger _logger;
        private readonly ITaskHistoryManager _taskHistoryManager;
        private readonly IConfigurationService _configurationService;
        private bool _disposed = false;

        /// <summary>
        /// 当用户请求退出应用时触发
        /// </summary>
        public event EventHandler? ExitRequested;

        /// <summary>
        /// 当配置更新时触发
        /// </summary>
        public event EventHandler? ConfigurationUpdated;

        /// <summary>
        /// 初始化TrayIconManager实例
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="taskHistoryManager">任务历史记录服务</param>
        /// <param name="configurationService">配置服务</param>
        public TrayIconManager(ILogger logger, ITaskHistoryManager taskHistoryManager, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _taskHistoryManager = taskHistoryManager ?? throw new ArgumentNullException(nameof(taskHistoryManager));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            // 创建托盘图标
            _trayIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Visible = true,
                Text = "打印店自动打印系统"
            };

            // 创建右键菜单
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("查看历史记录", null, OnViewHistoryClicked);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("配置", null, OnConfigurationClicked);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("退出", null, OnExitClicked);

            _trayIcon.ContextMenuStrip = _contextMenu;

            // 订阅鼠标点击事件
            _trayIcon.MouseClick += OnTrayIconMouseClick;
            
            // 更新托盘提示
            UpdateTrayTooltip();
            
            _logger.Info("托盘图标管理器已初始化");
        }

        /// <summary>
        /// 更新托盘提示文本（显示最近打印任务）
        /// </summary>
        public void UpdateTrayTooltip()
        {
            try
            {
                var recentTasks = _taskHistoryManager.GetRecentTasks(5);
                if (recentTasks.Count == 0)
                {
                    _trayIcon.Text = "打印店自动打印系统\n暂无打印任务历史";
                    return;
                }

                // 构建提示文本
                string tooltip = "打印店自动打印系统\n最近打印任务:\n";
                tooltip += string.Join("\n", recentTasks.Select(t => 
                    $"{t.Timestamp:HH:mm} - {t.FileCount}个文件 ({t.TotalPages}页)"));

                _trayIcon.Text = tooltip;
            }
            catch (Exception ex)
            {
                _logger.Error("更新托盘提示失败", ex);
                _trayIcon.Text = "打印店自动打印系统\n获取历史记录失败";
            }
        }

        /// <summary>
        /// 显示打印机选择对话框
        /// </summary>
        /// <param name="printers">可用打印机列表</param>
        /// <param name="fileCount">文件数量</param>
        /// <returns>用户选择的打印机名称，或null如果取消选择</returns>
        public string? ShowPrinterSelectionDialog(List<string> printers, int fileCount)
        {
            if (printers == null || printers.Count == 0)
            {
                _logger.Error("没有可用打印机，无法显示选择对话框");
                MessageBox.Show("没有可用的打印机，请检查打印机配置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            using (var dialog = new PrinterSelectionForm(printers, fileCount))
            {
                _logger.Info($"显示打印机选择对话框，共{printers.Count}台打印机");
                var result = dialog.ShowDialog();
                
                if (result == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPrinter))
                {
                    _logger.Info($"用户选择了打印机: {dialog.SelectedPrinter}");
                    return dialog.SelectedPrinter;
                }
                else
                {
                    _logger.Info("用户取消了打印机选择");
                    return null;
                }
            }
        }

        /// <summary>
        /// 显示任务历史窗口
        /// </summary>
        private void ShowTaskHistoryWindow()
        {
            try
            {
                var recentTasks = _taskHistoryManager.GetRecentTasks(10);
                using (var window = new TaskHistoryWindow(recentTasks))
                {
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("显示任务历史窗口失败", ex);
                MessageBox.Show("无法加载任务历史记录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 托盘图标鼠标点击事件处理
        /// </summary>
        private void OnTrayIconMouseClick(object sender, MouseEventArgs e)
        {
            // 左键点击显示任务历史
            if (e.Button == MouseButtons.Left)
            {
                ShowTaskHistoryWindow();
            }
        }

        /// <summary>
        /// 查看历史记录菜单项点击事件
        /// </summary>
        private void OnViewHistoryClicked(object sender, EventArgs e)
        {
            ShowTaskHistoryWindow();
        }

        /// <summary>
        /// 配置菜单项点击事件
        /// </summary>
        private void OnConfigurationClicked(object sender, EventArgs e)
        {
            try
            {
                _logger.Info("用户打开配置窗口");
                using (var configWindow = new ConfigurationWindow(_configurationService, _logger))
                {
                    if (configWindow.ShowDialog() == DialogResult.OK)
                    {
                        _logger.Info("配置已更新");
                        ConfigurationUpdated?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("打开配置窗口失败", ex);
                MessageBox.Show("无法打开配置窗口", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 退出菜单项点击事件
        /// </summary>
        private void OnExitClicked(object sender, EventArgs e)
        {
            _logger.Info("用户请求退出应用");
            ExitRequested?.Invoke(this, EventArgs.Empty);
            
            // 隐藏托盘图标
            _trayIcon.Visible = false;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 释放托管资源
                _contextMenu?.Dispose();
                _trayIcon?.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// 打印机选择对话框
    /// </summary>
    public class PrinterSelectionForm : Form
    {
        private readonly ListBox _printerListBox;
        private readonly Button _okButton;
        private readonly Button _cancelButton;
        private string? _selectedPrinter;

        /// <summary>
        /// 用户选择的打印机名称
        /// </summary>
        public string? SelectedPrinter => _selectedPrinter;

        /// <summary>
        /// 初始化PrinterSelectionForm实例
        /// </summary>
        /// <param name="printers">打印机列表</param>
        /// <param name="fileCount">文件数量</param>
        public PrinterSelectionForm(List<string> printers, int fileCount)
        {
            Text = "选择打印机";
            Size = new Size(350, 300);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // 创建控件
            var layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 3,
                ColumnCount = 1
            };

            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));

            // 提示标签
            var promptLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            promptLabel.Text = $"发现 {fileCount} 个新文件，请选择打印机:";
            layoutPanel.Controls.Add(promptLabel, 0, 0);

            // 打印机列表
            _printerListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 5)
            };
            
            // 添加打印机项
            foreach (var printer in printers)
            {
                _printerListBox.Items.Add(printer);
            }
            
            // 默认选择第一项
            if (_printerListBox.Items.Count > 0)
            {
                _printerListBox.SelectedIndex = 0;
            }
            
            layoutPanel.Controls.Add(_printerListBox, 0, 1);

            // 按钮面板
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 5, 0, 0)
            };

            // 取消按钮
            _cancelButton = new Button
            {
                Text = "取消",
                Size = new Size(75, 23),
                Margin = new Padding(5, 0, 0, 0)
            };
            _cancelButton.Click += (s, e) => DialogResult = DialogResult.Cancel;
            buttonPanel.Controls.Add(_cancelButton);

            // 确定按钮
            _okButton = new Button
            {
                Text = "确定",
                Size = new Size(75, 23),
                DialogResult = DialogResult.OK
            };
            _okButton.Click += (s, e) => 
            {
                _selectedPrinter = _printerListBox.SelectedItem?.ToString();
                DialogResult = DialogResult.OK;
            };
            buttonPanel.Controls.Add(_okButton);

            layoutPanel.Controls.Add(buttonPanel, 0, 2);
            Controls.Add(layoutPanel);

            // 设置默认按钮
            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }
    }

    /// <summary>
    /// 任务历史窗口
    /// </summary>
    public class TaskHistoryWindow : Form
    {
        /// <summary>
        /// 初始化TaskHistoryWindow实例
        /// </summary>
        /// <param name="taskRecords">任务记录列表</param>
        public TaskHistoryWindow(List<PrintTaskRecord> taskRecords)
        {
            Text = "打印任务历史";
            Size = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;

            // 创建数据网格
            var dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false
            };

            // 添加列
            dataGridView.Columns.Add("Timestamp", "时间");
            dataGridView.Columns.Add("FileCount", "文件数");
            dataGridView.Columns.Add("TotalPages", "总页数");
            dataGridView.Columns.Add("PrinterName", "打印机");

            // 添加行
            if (taskRecords != null && taskRecords.Count > 0)
            {
                foreach (var record in taskRecords)
                {
                    dataGridView.Rows.Add(
                        record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        record.FileCount,
                        record.TotalPages,
                        record.PrinterName
                    );
                }
            }
            else
            {
                dataGridView.Rows.Add("暂无历史记录", "", "", "");
            }

            // 添加关闭按钮
            var closeButton = new Button
            {
                Text = "关闭",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            closeButton.Click += (s, e) => Close();

            // 添加控件
            Controls.Add(dataGridView);
            Controls.Add(closeButton);
        }
    }

    /// <summary>
    /// 配置窗口
    /// </summary>
    public class ConfigurationWindow : Form
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger _logger;
        private AppSettings _settings;
        
        // 监视设置控件
        private TextBox? _watchPathTextBox;
        private Button? _browseButton;
        private TextBox? _fileTypesTextBox;
        private NumericUpDown? _timeoutNumericUpDown;
        
        // 打印机设置控件
        private CheckedListBox? _printersCheckedListBox;
        
        // 按钮
        private Button? _okButton;
        private Button? _cancelButton;

        /// <summary>
        /// 初始化ConfigurationWindow实例
        /// </summary>
        public ConfigurationWindow(IConfigurationService configurationService, ILogger logger)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = _configurationService.GetSettings();
            
            InitializeComponent();
            LoadSettings();
        }

        /// <summary>
        /// 初始化界面组件
        /// </summary>
        private void InitializeComponent()
        {
            Text = "系统配置";
            Size = new Size(700, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // 创建选项卡控件
            var tabControl = new TabControl { Dock = DockStyle.Fill };
            
            // 监视设置选项卡
            var monitoringTab = new TabPage("监视设置");
            CreateMonitoringTab(monitoringTab);
            tabControl.TabPages.Add(monitoringTab);
            
            // 打印机管理选项卡
            var printerTab = new TabPage("打印机管理");
            CreatePrinterTab(printerTab);
            tabControl.TabPages.Add(printerTab);
            
            // 文件类型关联选项卡
            var fileTypeTab = new TabPage("文件类型关联");
            CreateFileTypeTab(fileTypeTab);
            tabControl.TabPages.Add(fileTypeTab);
            
            mainPanel.Controls.Add(tabControl);
            
            // 按钮面板
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };
            
            _cancelButton = new Button
            {
                Text = "取消",
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel
            };
            _cancelButton.Location = new Point(buttonPanel.Width - 95, 10);
            
            _okButton = new Button
            {
                Text = "确定",
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _okButton.Location = new Point(_cancelButton.Left - 85, 10);
            _okButton.Click += OnOkClicked;
            
            buttonPanel.Controls.Add(_okButton);
            buttonPanel.Controls.Add(_cancelButton);
            
            Controls.Add(mainPanel);
            Controls.Add(buttonPanel);
            
            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        /// <summary>
        /// 创建监视设置选项卡
        /// </summary>
        private void CreateMonitoringTab(TabPage tab)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 4,
                ColumnCount = 3
            };
            
            // 设置行样式
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            
            // 设置列样式
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));

            // 监视文件夹
            var watchPathLabel = new Label
            {
                Text = "监视文件夹:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(watchPathLabel, 0, 0);

            _watchPathTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3)
            };
            panel.Controls.Add(_watchPathTextBox, 1, 0);

            _browseButton = new Button
            {
                Text = "浏览...",
                Dock = DockStyle.Fill,
                Margin = new Padding(3)
            };
            _browseButton.Click += OnBrowseClicked;
            panel.Controls.Add(_browseButton, 2, 0);

            // 文件类型
            var fileTypesLabel = new Label
            {
                Text = "文件类型:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(fileTypesLabel, 0, 1);

            _fileTypesTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                PlaceholderText = "例如: .pdf,.docx,.jpg"
            };
            panel.SetColumnSpan(_fileTypesTextBox, 2);
            panel.Controls.Add(_fileTypesTextBox, 1, 1);

            // 批量超时
            var timeoutLabel = new Label
            {
                Text = "批量超时(秒):",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(timeoutLabel, 0, 2);

            _timeoutNumericUpDown = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Minimum = 1,
                Maximum = 60,
                Value = 3
            };
            panel.SetColumnSpan(_timeoutNumericUpDown, 2);
            panel.Controls.Add(_timeoutNumericUpDown, 1, 2);

            // 说明文本
            var helpLabel = new Label
            {
                Text = "说明:\n" +
                       "• 监视文件夹：系统将监视此文件夹内的新文件\n" +
                       "• 文件类型：用逗号分隔的文件扩展名，如 .pdf,.docx,.jpg\n" +
                       "• 批量超时：文件停止变化后等待的秒数，然后开始打印处理",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = SystemColors.Info,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8)
            };
            panel.SetColumnSpan(helpLabel, 3);
            panel.Controls.Add(helpLabel, 0, 3);

            tab.Controls.Add(panel);
        }

        /// <summary>
        /// 创建打印机管理选项卡
        /// </summary>
        private void CreatePrinterTab(TabPage tab)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 2,
                ColumnCount = 1
            };
            
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 80f));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            // 打印机列表
            var printersLabel = new Label
            {
                Text = "取消勾选要隐藏的打印机:",
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _printersCheckedListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true
            };

            var printersPanel = new Panel { Dock = DockStyle.Fill };
            printersPanel.Controls.Add(_printersCheckedListBox);
            printersPanel.Controls.Add(printersLabel);
            printersLabel.Dock = DockStyle.Top;
            
            panel.Controls.Add(printersPanel, 0, 0);

            // 说明
            var printerHelpLabel = new Label
            {
                Text = "说明:\n取消勾选的打印机将不会在打印机选择对话框中显示。",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = SystemColors.Info,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8)
            };
            panel.Controls.Add(printerHelpLabel, 0, 1);

            tab.Controls.Add(panel);
        }

        /// <summary>
        /// 创建文件类型关联选项卡
        /// </summary>
        private void CreateFileTypeTab(TabPage tab)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 2,
                ColumnCount = 1
            };
            
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 70f));

            // 支持的文件类型显示
            var supportedTypesLabel = new Label
            {
                Text = "当前支持的文件类型:\n" +
                       "• PDF文件: .pdf\n" +
                       "• Word文档: .doc, .docx\n" +
                       "• 图片文件: .jpg, .jpeg, .png, .bmp, .gif, .tiff, .tif, .webp\n\n" +
                       "所有文件都将通过统一的PDF打印引擎处理",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = SystemColors.Info,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            panel.Controls.Add(supportedTypesLabel, 0, 0);

            // 说明
            var fileTypeHelpLabel = new Label
            {
                Text = "新架构说明:\n" +
                       "• 统一打印引擎：所有文件类型都先转换为PDF，然后通过PdfiumViewer打印\n" +
                       "• 图片处理：使用SkiaSharp进行高质量图片处理和PDF转换\n" +
                       "• Word文档：通过Microsoft Office Interop转换为PDF\n" +
                       "• PDF文件：直接打印，无需转换\n" +
                       "• 优势：统一的打印质量、更好的兼容性、更稳定的打印过程",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = SystemColors.Control,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };
            panel.Controls.Add(fileTypeHelpLabel, 0, 1);

            tab.Controls.Add(panel);
        }

        /// <summary>
        /// 加载当前设置到界面
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // 加载监视设置
                _watchPathTextBox.Text = _settings.Monitoring.WatchPath;
                _fileTypesTextBox.Text = string.Join(",", _settings.Monitoring.FileTypes);
                _timeoutNumericUpDown.Value = _settings.Monitoring.BatchTimeoutSeconds;

                // 加载打印机设置
                LoadPrinters();

                // 新架构不需要加载文件类型关联，因为是硬编码的转换器
                _logger.Debug("配置加载完成（使用新的统一打印架构）");
            }
            catch (Exception ex)
            {
                _logger.Error("加载配置失败", ex);
                MessageBox.Show("加载配置失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 加载打印机列表
        /// </summary>
        private void LoadPrinters()
        {
            try
            {
                _printersCheckedListBox.Items.Clear();
                
                // 获取系统所有打印机
                var allPrinters = new List<string>();
                foreach (string printerName in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    allPrinters.Add(printerName);
                }

                var hiddenPrinters = _settings.PrinterManagement.HiddenPrinters ?? new List<string>();

                // 添加到列表并设置勾选状态
                foreach (var printer in allPrinters)
                {
                    bool isVisible = !hiddenPrinters.Contains(printer);
                    _printersCheckedListBox.Items.Add(printer, isVisible);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("加载打印机列表失败", ex);
            }
        }

        /// <summary>
        /// 浏览文件夹按钮点击事件
        /// </summary>
        private void OnBrowseClicked(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择要监视的文件夹";
                dialog.SelectedPath = _watchPathTextBox.Text;
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _watchPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void OnOkClicked(object sender, EventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(_watchPathTextBox.Text))
                {
                    MessageBox.Show("请选择监视文件夹", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 保存设置
                SaveSettings();
                
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                _logger.Error("保存配置失败", ex);
                MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            // 保存监视设置
            _settings.Monitoring.WatchPath = _watchPathTextBox.Text.Trim();
            _settings.Monitoring.BatchTimeoutSeconds = (int)_timeoutNumericUpDown.Value;
            
            // 处理文件类型
            var fileTypesText = _fileTypesTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(fileTypesText))
            {
                _settings.Monitoring.FileTypes = fileTypesText
                    .Split(',')
                    .Select(ft => ft.Trim())
                    .Where(ft => !string.IsNullOrEmpty(ft))
                    .Select(ft => ft.StartsWith(".") ? ft : "." + ft)
                    .ToList();
            }

            // 保存打印机设置
            _settings.PrinterManagement.HiddenPrinters.Clear();
            for (int i = 0; i < _printersCheckedListBox.Items.Count; i++)
            {
                if (!_printersCheckedListBox.GetItemChecked(i))
                {
                    _settings.PrinterManagement.HiddenPrinters.Add(_printersCheckedListBox.Items[i].ToString());
                }
            }

            // 新架构不需要保存文件类型关联，因为使用内置转换器

            // 保存到配置文件
            _configurationService.SaveSettings(_settings);
        }
    }
}