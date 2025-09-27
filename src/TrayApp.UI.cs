using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TrayApp.Core;

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
        private bool _disposed = false;

        /// <summary>
        /// 当用户请求退出应用时触发
        /// </summary>
        public event EventHandler? ExitRequested;

        /// <summary>
        /// 初始化TrayIconManager实例
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="taskHistoryManager">任务历史记录服务</param>
        public TrayIconManager(ILogger logger, ITaskHistoryManager taskHistoryManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _taskHistoryManager = taskHistoryManager ?? throw new ArgumentNullException(nameof(taskHistoryManager));

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
}