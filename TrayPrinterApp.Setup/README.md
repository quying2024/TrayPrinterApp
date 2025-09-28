TrayPrinterApp.Setup

This folder is prepared to host a Visual Studio Installer Projects (vdproj) setup project.

What I added:
- This README (this file)
- A helper script `prepare-setup-files.ps1` that copies the self-contained publish output into the `TrayPrinterApp.Setup\ApplicationFolder` ready for adding to a Setup Project in Visual Studio.

How to finish in Visual Studio:
1. Run `.	ools\prepare-setup-files.ps1` (from repository root) after publishing to `publish\win-x64`.
2. In Visual Studio, right-click the solution -> Add -> New Project -> "Setup Project" (from Visual Studio Installer Projects extension). Name it `TrayPrinterApp.Setup` and create it inside this folder.
3. In the Setup Project: View -> File System. Set Application Folder DefaultLocation to `[ProgramFiles64Folder]\TrayPrinterApp`.
4. In Application Folder, use Add -> File and select the files under `TrayPrinterApp.Setup\ApplicationFolder` (prepared by the script).
5. Add `config` and `docs` folders via Add -> Folder and add desired files (README.md, LICENSE, docs/*.md).
6. Create shortcuts for `TrayApp.exe` under User's Desktop and User's Programs Menu.
7. Set Setup Project properties: TargetPlatform = x64, ProductName, Manufacturer, ProductVersion, RemovePreviousVersions = True.
8. Build the Setup Project to produce the MSI in the project's `bin\Release`.

Note: The script only prepares files for inclusion. The actual `.vdproj` file must be created by Visual Studio (the Installer Projects extension creates the correct format). If you prefer, I can try to generate a `.vdproj` file automatically, but Visual Studio normally manages those files and it's safer to create it inside the IDE.