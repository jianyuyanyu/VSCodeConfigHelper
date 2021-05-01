﻿// Copyright (C) 2020 Guyutongxue
// 
// This file is part of VSCodeConfigHelper.
// 
// VSCodeConfigHelper is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// VSCodeConfigHelper is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with VSCodeConfigHelper.  If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace VSCodeConfigHelper
{

    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();
            // 防止TabControl 切换时卡顿闪烁
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        // 防止TabControl 切换时卡顿闪烁
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        public static int minGWDistro = 0;
        public static MinGWLink ChosenMinGW {
            get{
                return minGWLinks[minGWDistro];
            }
        }

        static readonly MinGWLink[] minGWLinks = new MinGWLink[3]
        {
            MinGWLink.gytx,
            MinGWLink.tdm,
            MinGWLink.official
        };

        #region 路径们

        string workspacePath = string.Empty;
        string minGWPath = string.Empty;
        string vsCodePath = null;

        #endregion

        /// <summary>
        /// 存储所有可能的 MinGW 路径。
        /// </summary>
        List<string> minGWPathList = new List<string>();

        public static bool isMinGWDisk = true;
        bool isMinGWFirstTime = true;

        public static bool isSuccess = false;
        public static bool isCpp = true;
        public static string standard = "c++20";
        public static JArray args = GetDefaultArgs();
        public static JArray GetDefaultArgs()
        {
            return new JArray {
            "-g",
            "-std=" + standard,
            "\"${file}\"",
            "-o",
            "\"${fileDirname}\\${fileBasenameNoExtension}.exe\""
        };
}
        string FileExtension { get { return isCpp ? "cpp" : "c"; } }
        string Compiler { get { return isCpp ? "g++.exe" : "gcc.exe"; } }

        string TestCppCode
        {
            get
            {
                return @"// VS Code C++ 测试代码 ""Hello World""
// 由 VSCodeConfigHelper 生成

// 您可以在当前的文件夹（您第一步输入的文件夹）下编写代码。

// 按下 F5（部分设备上可能是 Fn + F5）编译调试。
// 按下 Ctrl + Shift + B 编译，但不运行。
// 按下 " + (IsInternal ? "Ctrl + F5（部分设备上可能是 Ctrl + Fn + F5）" : "F6（部分设备上可能是 Fn + F6）") + @"编译运行，但不调试。

#include <iostream>

/**
 * 程序执行的入口点。
 */
int main() {
    // 在标准输出中打印 ""Hello, world!""
    std::cout << ""Hello, world!"" << std::endl;
}

// 此文件编译运行将输出 ""Hello, world!""。
// 按下 " + (IsInternal ? "F5 后，您将在下方弹出的终端（Terminal）" : "F6 后，您将在弹出的") + @"窗口中看到这一行字。

// ** 重要提示：您以后编写其它代码时，请务必确保文件名不包含中文和特殊字符，切记！**

// 如果遇到了问题，请您浏览
// https://github.com/Guyutongxue/VSCodeConfigHelper/blob/master/TroubleShooting.md 
// 获取帮助。如果问题未能得到解决，请联系开发者。";
            }
        }
        string TestCCode
        {
            get
            {
                return @"/**
 * VS Code C 测试代码 ""Hello World""
 * 由 VSCodeConfigHelper 生成
 *
 * 您可以在当前的文件夹（您第一步输入的文件夹）下编写代码。
 *
 * 按下 F5（部分设备上可能是 Fn + F5）编译调试。
 * 按下 Ctrl + Shift + B 编译，但不运行。
 * 按下 " + (IsInternal ? "Ctrl + F5（部分设备上可能是 Ctrl + Fn + F5）" : "F6（部分设备上可能是 Fn + F6）") + @"编译运行，但不调试。
 *
 */

#include <stdio.h>
#include <stdlib.h>

/**
 * 程序执行的入口点。
 */
int main(void) {
    /* 在标准输出中打印 ""Hello, world!"" */
    printf(""Hello, world!"");
    return EXIT_SUCCESS;
}

/**
 * 此文件编译运行将输出 ""Hello, world!""。
 * 按下 " + (IsInternal ? "F5 后，您将在下方弹出的终端（Terminal）" : "F6 后，您将在弹出的") + @"窗口中看到这一行字。
 *
 * ** 重要提示：您以后编写其它代码时，请务必确保文件名不包含中文和特殊字符，切记！**
 *
 * 如果遇到了问题，请您浏览
 * https://github.com/Guyutongxue/VSCodeConfigHelper/blob/master/TroubleShooting.md
 * 获取帮助。如果问题未能得到解决，请联系开发者。
 * 
 */";
            }
        }

        readonly string consolePauserSrcCode = @"// This Code was licensed under GPL v2 by Bloodshed Dev-C++

// Execute & Pause
// Runs a program, then keeps the console window open after it finishes

#include <string>
using std::string;
#include <stdio.h>
#include <windows.h>

#define MAX_COMMAND_LENGTH 32768
#define MAX_ERROR_LENGTH 2048

LONGLONG GetClockTick() {
    LARGE_INTEGER dummy;
    QueryPerformanceCounter(&dummy);
    return dummy.QuadPart;
}

LONGLONG GetClockFrequency() {
    LARGE_INTEGER dummy;
    QueryPerformanceFrequency(&dummy);
    return dummy.QuadPart;
}

void PauseExit(int exitcode) {
    system(""pause"");
    exit(exitcode);
}

string GetErrorMessage() {
    string result(MAX_ERROR_LENGTH,0);
    
    FormatMessage(
        FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL,GetLastError(),MAKELANGID(LANG_NEUTRAL,SUBLANG_DEFAULT),&result[0],result.size(),NULL);
    
    // Clear newlines at end of string
    for(int i = result.length()-1;i >= 0;i--) {
        if(isspace(result[i])) {
            result[i] = 0;
        } else {
            break;
        }
    }
    return result;
}

string GetCommand(int argc,char** argv) {
    string result;
    for(int i = 1;i < argc;i++) {
        // Quote the first argument in case the path name contains spaces
//        if(i == 1) {
//            result += string(""\"""") + string(argv[i]) + string(""\"""");
//        } else {
            result += string(argv[i]);
//        }
        
        // Add a space except for the last argument
        if(i != (argc-1)) {
            result += string("" "");
        }
    }
    
    if(result.length() > MAX_COMMAND_LENGTH) {
        printf(""\n--------------------------------"");
        printf(""\nError: Length of command line string is over %d characters\n"",MAX_COMMAND_LENGTH);
        PauseExit(EXIT_FAILURE);
    }
    
    return result;
}

DWORD ExecuteCommand(string& command) {
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    memset(&si,0,sizeof(si));
    si.cb = sizeof(si);
    memset(&pi,0,sizeof(pi));
    
    if(!CreateProcess(NULL, (LPSTR)command.c_str(), NULL, NULL, false, 0, NULL, NULL, &si, &pi)) {
        printf(""\n--------------------------------"");
        printf(""\nFailed to execute \""%s\"":"",command.c_str());
        printf(""\nError %lu: %s\n"",GetLastError(),GetErrorMessage().c_str());
        PauseExit(EXIT_FAILURE);
    }
    WaitForSingleObject(pi.hProcess, INFINITE); // Wait for it to finish
    
    DWORD result = 0;
    GetExitCodeProcess(pi.hProcess, &result);
    return result;
}

int main(int argc, char** argv) {
    
    // First make sure we aren't going to read nonexistent arrays
    if(argc < 2) {
        printf(""\n--------------------------------"");
        printf(""\nUsage: ConsolePauser.exe <filename> <parameters>\n"");
        PauseExit(EXIT_SUCCESS);
    }
    
    // Make us look like the paused program
    SetConsoleTitle(argv[1]);
    
    // Then build the to-run application command
    string command = GetCommand(argc,argv);
    
    // Save starting timestamp
    LONGLONG starttime = GetClockTick();
    
    // Then execute said command
    DWORD returnvalue = ExecuteCommand(command);
    
    // Get ending timestamp
    LONGLONG endtime = GetClockTick();
    double seconds = (endtime - starttime) / (double)GetClockFrequency();

    // Done? Print return value of executed program
    printf(""\n--------------------------------"");
    printf(""\nProcess exited after %.4g seconds with return value %lu\n"",seconds,returnvalue);
    PauseExit(EXIT_SUCCESS);
}";

        public static bool IsRunningOn64Bit { get { return IntPtr.Size == 8; } }

        public static bool IsAdministrator
        {
            get
            {

                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public bool IsInternal { get { return radioButtonInternal.Checked; } }

        private void Form1_Load(object sender, EventArgs e)
        {
            Logging.Clear();
            Logging.Log($"Loading VSCodeConfigHelper, v{Application.ProductVersion}...");

            // 移除 TabPage 标签
            tabControlMain.ItemSize = new Size(0, 1);
            labelAuthor.Text = $"v{Application.ProductVersion} 谷雨同学制作 guyutongxue@163.com";

            // 异步检测更新
            new Thread(new ThreadStart(() => { FormSettings.CheckUpdate(false); })).Start();

            string specify = IsRunningOn64Bit ? "64" : "32";
            Logging.Log("System: " + specify + "bit");
            labelMinGWPathHint.Text = $"您解压后可以得到一个 mingw{specify} 文件夹。这里面包含着重要的编译必需文件，建议您将它移动到妥善的位置，如 C 盘根目录下。将它的路径输入在下面：";

            labelInternalHint.Text = "将使用 VS Code 自带的终端显示程序的输出。您将在代码区的下方看到这个内置终端，从而查看运行和调试的输出。" + Environment.NewLine +
                "因此您无需手动在程序结束之后暂停，您可以随时查看已有的程序输出。但是您可能在查看较长的输出时略显费力。" + Environment.NewLine +
                "这是 VS Code 推荐的输出方式。除非您特有需求，否则建议您选择此样式。";
            labelExternalHint.Text = "将使用 Windows 的终端窗口显示程序的输出。当您运行或调试时，将弹出终端窗口以显示程序输出。" + Environment.NewLine +
                "程序将在运行结束后暂停，但调试结束时不会暂停。这种方式方便您查看长输出以及代码运行的时间。" + Environment.NewLine +
                "这种样式会改变您的全局快捷键设置，因此可能导致冲突。请谨慎使用。";

            // isMinGWDisk = DateTime.Now.Date < new DateTime(2024, 10, 1);

            if (IsAdministrator) Text = "管理员: VS Code C++配置工具";
            Logging.Log("Administrator: " + (IsAdministrator ? "Yes" : "No"));

            // 检测并读取缓存
            if (File.Exists("VSCHcache.txt"))
            {
                Logging.Log("Cache detected.");
                StreamReader sr = new StreamReader("VSCHcache.txt");
                try
                {
                    JArray cache = (JArray)JsonConvert.DeserializeObject(sr.ReadToEnd());
                    if (cache == null) throw new Exception("JSON object is null.");
                    textBoxMinGWPath.Text = (string)cache[0];
                    textBoxWorkspacePath.Text = (string)cache[1];
                    isMinGWDisk = (bool)cache[2];
                    minGWDistro = (int)cache[3];
                    isCpp = (bool)cache[4];
                    standard = (string)cache[5];
                    args = (JArray)cache[6];
                    Logging.Log("Cache reading done.");
                }
                catch (Exception ex)
                {
                    Logging.Log($"Cache reading failed. Message: {ex.Message}", LogType.Error);
                }
                finally
                {
                    sr.Close();
                }
            }
            Logging.Log("VSCodeConfigHelper main form loaded successfully.");
        }

        private void ButtonViewMinGW_Click(object sender, EventArgs e)
        {
            Logging.Log("User is selecting MinGW by Folder Dialog...");
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxMinGWPath.Text = folderBrowserDialog1.SelectedPath;
            }
            Logging.Log($"User selected {folderBrowserDialog1.SelectedPath} as MinGW Path.");
        }

        private void TextBoxMinGWPath_TextChanged(object sender, EventArgs e)
        {
            buttonMinGWNext.Enabled = false;
            minGWPath = textBoxMinGWPath.Text;
            if (!string.IsNullOrWhiteSpace(minGWPath))
            {
                if (!Regex.IsMatch(minGWPath, "^[!-~]*$"))
                {
                    labelMinGWState.ForeColor = Color.Red;
                    labelMinGWState.Text = "请保证路径中不包含空格、中文和特殊字符。";
                }
                else if (Directory.Exists(minGWPath) && File.Exists(minGWPath + "\\bin\\g++.exe"))
                {
                    labelMinGWState.ForeColor = Color.Green;
                    labelMinGWState.Text = "检测到编译器：";
                    string version = GetGxxVersion(minGWPath + "\\bin\\g++.exe");
                    labelMinGWState.Text += '\n' + version;
                    buttonMinGWNext.Enabled = true;
                }
                else
                {
                    labelMinGWState.ForeColor = Color.Red;
                    labelMinGWState.Text = "未检测到编译器，请重试。";
                }
            }

        }

        private void listViewMinGW_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isMinGWFirstTime)
            {
                buttonMinGWNext.Enabled = listViewMinGW.SelectedItems.Count == 1;
            }
        }

        private void CheckCurrentMinGW()
        {
            EnvironmentVariableTarget current = IsAdministrator ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
            string[] paths = Environment.GetEnvironmentVariable("Path", current).Split(Path.PathSeparator);
            minGWPathList.Clear();
            foreach (var i in paths)
            {
                if (File.Exists(i + "\\" + Compiler))
                {
                    minGWPathList.Add(i);
                }
            }
            isMinGWFirstTime = minGWPathList.Count == 0;
            panelMinGWTable.Visible = !isMinGWFirstTime;
            if (isMinGWFirstTime)
            {
                labelMinGWHint.Text = "您还没有安装 MinGW，请您点击右侧链接下载。";
                Logging.Log("No MinGW detected.");
            }
            else
            {
                labelMinGWHint.Text = "您已安装下列编译环境，请选中其中一个作为您的配置。您也可以点击右侧链接下载推荐的 MinGW-w64 环境。";
                listViewMinGW.Items.Clear();
                Logging.Log("MinGW Detected as below:");
                foreach (var i in minGWPathList)
                {
                    ListViewItem lvi = GenerateMinGWLVItem(i);
                    listViewMinGW.Items.Add(lvi);
                    Logging.Log(lvi.Text + "\t" + lvi.SubItems[1].Text + "\t" + lvi.SubItems[2].Text);
                }
                if (listViewMinGW.Items.Count == 1) listViewMinGW.Items[0].Selected = true;
                buttonMinGWNext.Enabled = listViewMinGW.SelectedItems.Count == 1;
            }
        }

        ListViewItem GenerateMinGWLVItem(string path)
        {

            ListViewItem lvi = new ListViewItem();
            lvi.Text = GuessDescription(path + "\\" + Compiler, out string hint);
            if (hint != null)
            {
                lvi.ForeColor = Color.Red;
                lvi.ToolTipText = hint;
            }
            lvi.SubItems.Add(path.Substring(0, path.Length - 4));
            lvi.SubItems.Add(GetGxxVersion(path + "\\" + Compiler) ?? "");
            return lvi;
        }

        /// <summary>
        /// 输出某编译环境猜测的版本（发行版）
        /// </summary>
        /// <param name="path">编译器的路径</param>
        /// <param name="hint">如果该编译器兼容性较差，则输出提示</param>
        /// <returns></returns>
        private string GuessDescription(string path, out string hint)
        {
            string shortVersion = GetGxxVersion(path);
            if (shortVersion is null)
            {
                hint = null;
                return "Unknown";
            }
            string distribute;
            string versionNumber = Regex.Match(shortVersion, " [^ ]+$").Value;
            hint = null;
            if (shortVersion.Contains("tdm64"))
            {
                distribute = "TDM-GCC64";
                if (!IsRunningOn64Bit) hint = "该编译环境并非为 32 位系统设计，可能导致错误。";
            }
            else if (shortVersion.Contains("tdm"))
            {
                distribute = "TDM-GCC";
                if (IsRunningOn64Bit) hint = "该编译环境并非为 64 位系统设计，可能导致错误。";
            }
            else if (shortVersion.Contains("MinGW-W64"))
            {
                if (shortVersion.Contains("x86_64"))
                {

                    distribute = "MinGW-w64";
                    if (!IsRunningOn64Bit) hint = "该编译环境并非为 32 位系统设计，可能导致错误。";
                }
                else
                {
                    distribute = "MinGW-w64(i686)";
                    if (IsRunningOn64Bit) hint = "该编译环境并非为 64 位系统设计，可能导致错误。";
                }
            }
            else if (shortVersion.Contains("MinGW.org"))
            {
                distribute = "MinGW.org";
                if (IsRunningOn64Bit) hint = "该编译环境并非为 64 位系统设计，可能导致错误。";
            }
            else
            {
                string longVersion = GetGxxVersion(path, true);
                distribute = Regex.Match(longVersion, "(?<=Target: ).*$", RegexOptions.Multiline).Value;
                // distribute = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(distribute);
            }
            try
            {
                if (int.Parse(versionNumber.Split('.').First()) < 5)
                {
                    hint = "编译器版本较老，可能无法正常工作。";
                }
                // Fix standard specification for older version of GCC
                if (int.Parse(versionNumber.Split('.').First()) < 10)
                {
                    standard = "c++17";
                    args = GetDefaultArgs();
                }
            }
            catch (Exception)
            {
                // who cares?
            }
            if (!Regex.IsMatch(path, "^[!-~]*$"))
            {
                hint = "此 MinGW 存放路径包含空格、中文或特殊符号，可能导致出现问题。";
            }
            return distribute + " " + versionNumber;
        }

        private static string GetGxxVersion(string path, bool verbose = false)
        {

            string result = null;
            using (Process proc = new Process())
            {
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.FileName = path;
                proc.StartInfo.Arguments = verbose ? "-v" : "--version";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                proc.WaitForExit();
                if (verbose) result = proc.StandardError.ReadToEnd();
                else result = proc.StandardOutput.ReadLine();
                proc.Close();
            }
            return result;
        }

        private void buttonMinGWAdd_Click(object sender, EventArgs e)
        {
            Logging.Log("User choose a new MinGW while already existing ones...");
            string result = folderBrowserDialog1.SelectedPath;
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return;
            Logging.Log($"User choosed {result} as a MinGW Path.");
            if (File.Exists(result + "\\bin\\" + Compiler))
            {
                listViewMinGW.Items.Add(GenerateMinGWLVItem(result + "\\bin"));
                minGWPathList.Add(result);
                Logging.Log("The chosen MinGW path has been added to the ListView.");
            }
            else if (File.Exists(result + "\\" + Compiler))
            {
                listViewMinGW.Items.Add(GenerateMinGWLVItem(result));
                minGWPathList.Add(result.Substring(0, result.Length - 4));
                Logging.Log("The chosen MinGW path has been added to the ListView.");
            }
            else
            {
                MessageBox.Show("未检测到有效编译器。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.Log("The chosen MinGW path is unavailble. Failed to add the path to ListView. Continue.", LogType.Error);
            }
        }


        private void ButtonExtension_Click(object sender, EventArgs e)
        {
            // string cppLink = @"https://marketplace.visualstudio.com/items?itemName=ms-vscode.cpptools";
            // Process.Start(cppLink);
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = "cmd";
                proc.StartInfo.Arguments = "/C \"" + vsCodePath.ToLower().Replace("code.exe", "bin\\code.cmd") + "\" --install-extension ms-vscode.cpptools & pause";
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
                Logging.Log("Install C/C++ extension by executing following command. Maximum waiting time is 30s.");
                Logging.Log($"\"{proc.StartInfo.FileName}\" {proc.StartInfo.Arguments}");
                proc.WaitForExit(30000);
                proc.Close();
                Logging.Log("Execute finished.");
            }
            CheckExtension();
        }

        private void LinkLabelMinGW_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string link = ChosenMinGW.getLink(IsRunningOn64Bit, isMinGWDisk);
            Logging.Log("User clicked MinGW download link: " + link);
            Process.Start(link);
        }


        private void LinkLabelVSCode_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Logging.Log("User clicked VS Code download link.");
                string adminSpec = IsAdministrator ? "" : "-user";
                string bitSpec = IsRunningOn64Bit ? "-x64" : "";
                Process.Start("https://update.code.visualstudio.com/latest/win32" + bitSpec + adminSpec + "/stable");
            }
            catch (Exception)
            {
                // Shouldn't be executed
                MessageBox.Show("无法获得直接下载地址，请手动点击下载" + (IsAdministrator ? " System 版本安装包" : "") + "。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.Start("https://code.visualstudio.com/Download");
            }
            // Hint image (Open by browser)
            // No use now
            // Process.Start("https://s2.ax1x.com/2020/01/18/1pRERI.png");
        }

        private void ButtonViewWorkspace_Click(object sender, EventArgs e)
        {
            Logging.Log("User is selecting workspace folder path...");
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxWorkspacePath.Text = folderBrowserDialog1.SelectedPath;
                Logging.Log($"User selected {folderBrowserDialog1.SelectedPath} as workspace folder.");
            }
        }

        private JObject GetLaunchJson()
        {
            JObject command = new JObject
            {
                {"description", "Enable pretty-printing for gdb"},
                {"text", "-enable-pretty-printing"},
                {"ignoreFailures", true}
            };
            JObject config = new JObject
            {
                {"name", Compiler + " build and debug active file"},
                {"type", "cppdbg"},
                {"request", "launch"},
                {"program", "${fileDirname}\\${fileBasenameNoExtension}.exe"},
                {"args", new JArray()},
                {"stopAtEntry", false},
                {"cwd", "${workspaceFolder}"},
                {"environment", new JArray()},
                {"externalConsole", !IsInternal},
                {"MIMode", "gdb"},
                {"miDebuggerPath", minGWPath+"\\bin\\gdb.exe"},
                {"setupCommands",new JArray{command} },
                {"preLaunchTask", Compiler + " build active file" },
                {"internalConsoleOptions", "neverOpen" }
            };

            JObject launch = new JObject
            {
                { "version", "0.2.0" },
                {"configurations",new JArray{config} }
            };
            return launch;
        }

        private JObject GetTasksJson()
        {
            JObject group = new JObject
            {
                {"kind", "build"},
                {"isDefault", true}
            };
            JObject presentation = new JObject
            {
                {"echo", false},
                {"reveal", "silent"},
                {"focus", false},
                {"panel", "shared"},
                {"showReuseMessage", false},
                {"clear", true}
            };

            JArray taskList = new JArray
            {
                new JObject
                    {
                        {"type", "shell"},
                        {"label", Compiler + " build active file"},
                        {"command", minGWPath + "\\bin\\" + Compiler},
                        {"args",args},
                        {"group",group},
                        {"presentation",presentation},
                        {"problemMatcher",new JArray{ "$gcc" } }
                    }
            };
            // Add another task for external running
            if (!IsInternal)
            {
                taskList.Add(new JObject
                {
                    { "label", "run_pause" },
                    { "type", "shell" },
                    { "command", "cmd"},
                    { "dependsOn", Compiler + " build active file"},
                    { "args", new JArray {
                        "/C",
                        "START",
                        minGWPath + "\\bin\\ConsolePauser.exe",
                        "\"${fileDirname}\\${fileBasenameNoExtension}.exe\""
                    }},
                    { "presentation", new JObject {
                        { "reveal", "never" }
                    }},
                    { "problemMatcher", new JArray()},
                    { "group", new JObject{
                        { "kind", "test" },
                        { "isDefault", true }
                    }}
                });
            }

            JObject tasks = new JObject
            {
                { "version","2.0.0" },
                { "tasks",taskList },
                // https://github.com/microsoft/vscode/issues/70509
                { "options",new JObject
                    { 
                        {
                            "shell", new JObject
                            {
                                { "executable", "${env:SystemRoot}\\System32\\cmd.exe" },
                                { "args", new JArray("/c") }
                            }
                        
                        }, { "env", new JObject
                            {
                                {"Path", minGWPath + "\\bin:${env:Path}" }
                            } 
                        }
                    }
                }
            };
            return tasks;
        }

        private JObject GetSettingsJson()
        {
            return new JObject
            {
                {"C_Cpp.default.intelliSenseMode", "gcc-x" + (IsRunningOn64Bit ? "64" : "86")},
                {"C_Cpp.default.compilerPath", minGWPath + "\\bin\\" + Compiler},
                {"C_Cpp.default."+(isCpp?"cpp":"c")+"Standard", standard},
                {"debug.onTaskErrors", "abort" }
            };
        }

        private string GenerateTestFile(string path)
        {
            string filepath = $"{path}\\helloworld." + FileExtension;
            if (File.Exists(filepath))
            {
                for (int i = 1; ; i++)
                {
                    filepath = $"{path}\\helloworld({i})." + FileExtension;
                    if (!File.Exists(filepath)) break;
                }
            }
            // Remove BOM
            StreamWriter sw = new StreamWriter(filepath, false, new UTF8Encoding(false));
            Logging.Log($"Writing test code file at {filepath}");
            if (isCpp) sw.Write(TestCppCode);
            else sw.Write(TestCCode);
            sw.Flush();
            sw.Close();
            Logging.Log("Test code written.");
            return filepath;
        }

        private string GetVSCodePath(bool crossTest = false)
        {
            Logging.Log("Detecting VS Code path...");
            RegistryKey root;
            if (IsAdministrator ^ crossTest)
            {
                root = Registry.LocalMachine;
                Logging.Log("Scanning Registry HKLM...");
            }
            else
            {
                root = Registry.CurrentUser;
                Logging.Log("Scanning Regitry HKCU...");
            }
            RegistryKey rk = root.OpenSubKey("SOFTWARE\\Classes\\Applications\\Code.exe\\shell\\open\\command");
            if (rk == null)
            {
                Logging.Log("Key not found. VS Code may not installed.");
                return null;
            }
            string value = (string)rk.GetValue("", null);
            Logging.Log("Value of the registry key: " + value);
            // The value should be like:
            // "C:\Program Files\Microsoft VS Code\Code.exe" --open-url -- "%1"
            // and we just use the string inside the first quatation marks
            value = value.Split('"')[1];
            if (!File.Exists(value)) return null;
            return value;
        }

        private void LoadVSCode(string folderpath, string filepath = null)
        {
            using (Process proc = new Process())
            {
                // proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.CreateNoWindow = true;
                // proc.StartInfo.UseShellExecute = true;
                if (string.IsNullOrEmpty(vsCodePath))
                    throw new Exception("VS Code path not found.");
                proc.StartInfo.FileName = vsCodePath;
                if (string.IsNullOrEmpty(filepath))
                {
                    proc.StartInfo.Arguments = $"\"{folderpath}\"";
                }
                else
                {
                    proc.StartInfo.Arguments = $" -g \"{filepath}:1\" \"{folderpath}\"";
                }
                Logging.Log("Launching VS Code by the following command:");
                Logging.Log($"\"{vsCodePath}\" {proc.StartInfo.Arguments}");
                proc.Start();
                // proc.WaitForExit();
                proc.Close();
            }
        }


        private void LinkLabelManual_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Logging.Log("User clicked manual link.");
            string manualLink = @"https://github.com/Guyutongxue/VSCodeConfigHelper/blob/master/README.md";
            Process.Start(manualLink);
        }

        private void TextBoxWorkspacePath_TextChanged(object sender, EventArgs e)
        {
            workspacePath = textBoxWorkspacePath.Text;
            if (string.IsNullOrWhiteSpace(workspacePath))
            {
                buttonWelcomeNext.Enabled = false;
                labelWorkspaceStatus.Visible = false;
                return;
            }
            if (!Regex.IsMatch(workspacePath, "^[!-~]*$"))
            {
                buttonWelcomeNext.Enabled = false;
                labelWorkspaceStatus.Visible = true;
            }
            else
            {
                buttonWelcomeNext.Enabled = true;
                labelWorkspaceStatus.Visible = false;
            }
            // If the helloworld.cpp exists already, then do not generate by default
            if (Directory.Exists(textBoxWorkspacePath.Text) &&
                File.Exists(textBoxWorkspacePath.Text + "\\helloworld." + FileExtension))
            {
                checkBoxGenTest.Checked = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isSuccess)
            {
                Logging.Log("User try to abort this configure...");
                DialogResult dr = MessageBox.Show("确定中止本次配置？您所有已进行的配置将被保存。", "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (dr == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    Logging.Log("User cancel the operation. Configure continued.");
                    return;
                }
                Logging.Log("Saving cache of this configure...");
                StreamWriter sw = new StreamWriter("VSCHcache.txt");
                try
                {
                    JArray cache = new JArray
                    {
                        minGWPath,
                        workspacePath,
                        isMinGWDisk,
                        minGWDistro,
                        isCpp,
                        standard,
                        args
                    };
                    sw.WriteLine(cache.ToString(Formatting.Indented));
                    sw.Flush();
                    Logging.Log("Cache saved. Program will exit.");
                }
                catch (Exception ex)
                {
                    Logging.Log($"Cache saving failed. Message:{ex.Message}", LogType.Error);
                }
                finally
                {
                    sw.Close();
                }
            }
            else
            {
                //if (File.Exists("VSCHcache.txt"))
                //{
                //    File.Delete("VSCHcache.txt");
                //}
                Logging.Log("User has successfully configured, and the form is closing.");
            }
        }

        private void buttonWelcomeNext_Click(object sender, EventArgs e)
        {
            Logging.Log("User click next on Welcome page.");
            if (Directory.Exists(workspacePath + "\\.vscode"))
            {
                Logging.Log(".vscode already exists in " + workspacePath, LogType.Warning);
                DialogResult result = MessageBox.Show("检测到已有配置，是否立即移除它们？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (result == DialogResult.Cancel) return;
                Directory.Delete(workspacePath + "\\.vscode", true);
                Logging.Log($"old .vscode folder at {workspacePath} removed.");
            }
            workspacePath = textBoxWorkspacePath.Text;
            tabControlMain.SelectedIndex++;
        }

        private void buttonPrev_Click(object sender, EventArgs e)
        {
            tabControlMain.SelectedIndex--;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonMinGWNext_Click(object sender, EventArgs e)
        {
            try
            {
                Logging.Log("User click next on MinGW Page. Dealing with MinGW...");
                buttonMinGWNext.Enabled = false;
                buttonMinGWNext.Text = "正在设置...";
                Application.DoEvents();
                EnvironmentVariableTarget current = IsAdministrator ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
                string path = Environment.GetEnvironmentVariable("Path", current);
                if (isMinGWFirstTime)
                {
                    string distro = GuessDescription(minGWPath + "\\bin\\" + Compiler, out string hint);
                    Logging.Log($"First Time Configure; MinGWPath is {minGWPath}");
                    Logging.Log($"MinGW distro version is {distro}");
                    if (hint != null)
                    {
                        DialogResult dr = MessageBox.Show($"您选择的 MinGW 环境不推荐，因为：{hint} 确定继续吗？", "警告", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                        Logging.Log($"Distro not recommended, because {hint}", LogType.Warning);
                        if (dr == DialogResult.Cancel) return;
                    }
                    Environment.SetEnvironmentVariable("Path", path + ";" + minGWPath + "\\bin", current);
                    Logging.Log("Add MinGW to the PATH successfully. PATH listed below:");
                    Logging.Log(path.Replace(Path.PathSeparator.ToString(), Environment.NewLine), LogType.Multiline);
                }
                else
                {
                    if (!string.IsNullOrEmpty(listViewMinGW.SelectedItems[0].ToolTipText))
                    {
                        DialogResult dr = MessageBox.Show($"您选择的 MinGW 环境不推荐，因为：{listViewMinGW.SelectedItems[0].ToolTipText} 确定继续吗？", "警告", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                        Logging.Log($"Distro not recommended, because {listViewMinGW.SelectedItems[0].ToolTipText}", LogType.Warning);
                        if (dr == DialogResult.Cancel) return;
                    }
                    minGWPath = listViewMinGW.SelectedItems[0].SubItems[1].Text;
                    Logging.Log("Selected MinGW Path: " + minGWPath);
                    // If only one in the list, just ignore.
                    // Because the ony one is detected from PATH, and there is no need to remove or add.
                    if (minGWPathList.Count > 1)
                    {
                        Logging.Log("More than one path, prepare to remove others from PATH.");
                        DialogResult dr = MessageBox.Show("程序将从 PATH 环境变量中移除其余 MinGW 环境，但不会删除您的任何文件。确定继续吗？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                        if (dr == DialogResult.Cancel) return;

                        List<string> splitedPath = new List<string>(path.Split(Path.PathSeparator));
                        for (int i = 0; i < splitedPath.Count;)
                        {
                            if (minGWPathList.Contains(splitedPath[i])) splitedPath.RemoveAt(i);
                            else i++;
                        }
                        string result = string.Empty;
                        foreach (var i in splitedPath)
                        {
                            result += i + Path.PathSeparator;
                        }
                        result += minGWPath + "\\bin";
                        Environment.SetEnvironmentVariable("Path", result, current);
                        Logging.Log("PATH is ready. Listed below:");
                        Logging.Log(result.Replace(Path.PathSeparator.ToString(), Environment.NewLine), LogType.Multiline);
                    }
                }
                tabControlMain.SelectedIndex++;
            }
            catch(Exception)
            {
                Logging.Log("Seems that error occurs when adding PATH.", LogType.Error);
                Logging.Log(@"Try with following steps to skip this:
1. 启动控制面板 -> 系统和安全 -> 系统；
2. 点击左侧“高级系统设置”，点击“环境变量(N)...”按钮；
3. 选中" + (IsAdministrator ? "下方系统变量中" : "上方用户变量中") + @"的 Path 项，点击对应的“编辑”按钮；
4. 在您的 MinGW 路径后添加 '\bin'（比如“C:\mingw64\bin”），然后加入至这个环境变量中。如果您使用 Windows 7，请在路径之间用分号 ';' 分隔。
5. 保存全部设置，重新启动本工具。", LogType.Multiline);
                throw;
            }
            finally
            {
                buttonMinGWNext.Enabled = true;
                buttonMinGWNext.Text = "下一步";
            }
        }

        void CheckExtension()
        {
            Logging.Log("Checking if C/C++ extension installed...");
            bool isExtensionOk = false;
            using (Process proc = new Process())
            {
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                // The command line aparameter should be passed to batch file not the exe
                proc.StartInfo.FileName = vsCodePath.ToLower().Replace("code.exe", "bin\\code.cmd");
                proc.StartInfo.Arguments = "--list-extensions";
                proc.StartInfo.RedirectStandardOutput = true;
                Logging.Log($"Execute command (maximum waiting time 5s):");
                Logging.Log($"\"{proc.StartInfo.FileName}\" {proc.StartInfo.Arguments}");
                proc.Start();
                // System.Threading.Thread.Sleep(1000);
                proc.WaitForExit(5000);
                string result = string.Empty;
                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();
                    if (line == "ms-vscode.cpptools")
                    {
                        isExtensionOk = true;
                    }
                    result += line + Environment.NewLine;
                }
                Logging.Log($"Execute finished. Exit code is {proc.ExitCode}, while output:");
                Logging.Log(result, LogType.Multiline);
                Logging.Log("C/C++ Extension" + (isExtensionOk ? " " : " not ") + "installed.");
                proc.Close();
            }
            if (isExtensionOk)
            {
                labelExtensionHint.Text = "C/C++ 扩展已安装好。点击下一步以继续。";
                buttonExtension.Enabled = false;
                buttonExtension.Text = "已安装";
                buttonCodeNext.Enabled = true;
            }
            else
            {
                labelExtensionHint.Text = "请点击左侧按钮安装 C/C++ 扩展。";
                buttonExtension.Enabled = true;
                buttonExtension.Text = "安装扩展";
                buttonCodeNext.Enabled = false;
            }
        }

        void CheckCodeAndExtension()
        {
            Logging.Log("Checking VS Code installation...");
            labelCodeHint.Text = labelExtensionHint.Text = "正在自动检测，请稍候...";
            labelCodeHint.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
            buttonCodeNext.Enabled = buttonExtension.Enabled = buttonRefresh.Enabled = false;
            vsCodePath = GetVSCodePath();
            Logging.Log("Check if VS Code installed in wrong places...");
            string alternativePathToVSCode = GetVSCodePath(true);
            if (!string.IsNullOrEmpty(vsCodePath))
            {
                Logging.Log("VS Code Installed correctly at " + vsCodePath);
                labelCodeHint.Text = "检测到已安装VS Code。位于：" + Environment.NewLine + vsCodePath;
                buttonRefresh.Enabled = false;
                buttonRefresh.Text = "已安装";
                CheckExtension();
            }
            else if (!string.IsNullOrEmpty(alternativePathToVSCode))
            {
                vsCodePath = alternativePathToVSCode;
                Logging.Log($"VS Code Installed at {vsCodePath}, but with wrong authority.", LogType.Warning);
                labelCodeHint.Text = "检测到 VS Code，但是该版本可能与 MinGW 配置发生冲突。建议您点击右侧链接重新安装。";
                labelCodeHint.ForeColor = Color.Red;
                buttonRefresh.Enabled = true;
                buttonRefresh.Text = "刷新";
                CheckExtension();
            }
            else
            {
                Logging.Log("VS Code not installed.");
                labelCodeHint.Text = "未检测到已安装的VS Code。" + Environment.NewLine + "请点击右侧地址下载安装。" + Environment.NewLine + "（若您已安装但未检测到，请您卸载并通过右侧地址重新安装。）";
                labelExtensionHint.Text = "请点击左侧按钮安装 C/C++ 扩展。";
                buttonExtension.Enabled = false;
                buttonExtension.Text = "安装扩展";
                buttonRefresh.Enabled = true;
                buttonRefresh.Text = "刷新";
                buttonCodeNext.Enabled = false;
            }
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            Logging.Log("User launched settings form.");
            FormSettings formSettings = new FormSettings();
            formSettings.ShowDialog();
        }

        private void tabControlMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            panelNavigate.Refresh();
            switch (tabControlMain.SelectedIndex)
            {
                case 0:
                    Logging.Log("User switched to Welcome page.");
                    break;
                // If switch to page MinGW, check the Path
                case 1:
                    Logging.Log("User switched to MinGW page.");
                    CheckCurrentMinGW();
                    break;
                // If switch to page Code, check the installation
                case 2:
                    Logging.Log("User switched to Code/Extension page.");
                    CheckCodeAndExtension();
                    break;
                case 3:
                    Logging.Log("User switched to Style page.");
                    break;
                case 4:
                    Logging.Log("User switched to Finish page.");
                    break;
            }
        }
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            if(ModifierKeys == Keys.Shift)
            {
                DialogResult dr = MessageBox.Show("您正在手动选择 VS Code 路径。此设置是临时的，将不会保留。是否继续？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Cancel) return;
                Logging.Log("User try to select VS Code path manually.");
                openFileDialog1.Filter = "VS Code Executable(code.exe)|code.exe";
                openFileDialog1.Title = "选择 code.exe 路径";
                openFileDialog1.DefaultExt = ".exe";
                if(openFileDialog1.ShowDialog()==DialogResult.OK)
                {
                    vsCodePath = openFileDialog1.FileName;
                    labelCodeHint.Text = "用户选择的 VS Code 路径：" + vsCodePath + "（临时）";
                    Logging.Log("User selected VS Code Path: " + vsCodePath);
                    labelCodeHint.ForeColor = Color.Red;
                    CheckExtension();
                }
            } else
            {
                CheckCodeAndExtension();
            }
        }


        private void buttonCodeNext_Click(object sender, EventArgs e)
        {
            tabControlMain.SelectedIndex++;
            Logging.Log("User clicked next on Code/Extension page.");
        }

        void CompileConsolePauser()
        {
            Logging.Log("Preparing to compile ConsolePauser.cpp...");
            string tempPath = Path.GetTempPath();
            StreamWriter sw = new StreamWriter(tempPath + "\\ConsolePauser.cpp");
            sw.Write(consolePauserSrcCode);
            sw.Flush();
            sw.Close();
            Logging.Log($"Source code written in ${tempPath}\\ConsolePauser.cpp");

            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = minGWPath + "\\bin\\g++.exe";
                proc.StartInfo.Arguments = $"\"{tempPath}\\ConsolePauser.cpp\" -o \"{minGWPath}\\bin\\ConsolePauser.exe\"";
                proc.StartInfo.CreateNoWindow = true;
                Logging.Log($"Compiling command:");
                Logging.Log($"\"{proc.StartInfo.FileName}\" {proc.StartInfo.Arguments}");
                proc.Start();
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    Logging.Log("Compilation failed. Exception throwed.", LogType.Error);
                    throw new Exception("Compilation Error.");
                }
                Logging.Log("Compilation successed.");
            }

        }

        JArray GetKeyboardBindingJson()
        {
            return new JArray
            {
                new JObject{
                    { "key", "f6" },
                    { "command", "workbench.action.tasks.runTask" },
                    { "args", "run_pause" }
                }
            };
        }

        private void buttonConfigNext_Click(object sender, EventArgs e)
        {
            Logging.Log("User clicked next on Configuration(Style) page.");
            Logging.Log("Is internal style: " + (IsInternal ? "Yes" : "No"));
            isSuccess = false;
            try
            {
                JObject launchJson = GetLaunchJson();
                JObject tasksJson = GetTasksJson();
                JObject settingsJson = GetSettingsJson();

                // Since 2.2.11, applying env is not necessary.
                //
                //Logging.Log("Killing the vscode process...");
                //// Kill VS Code process to apply PATH env and prevent occupy
                //Process[] processList = Process.GetProcesses();
                //foreach (var process in processList)
                //{
                //    if (process.ProcessName.ToLower() == "code")
                //    {
                //        process.Kill();
                //        Logging.Log($"PID {process.Id} killed.");
                //    }
                //}

                if (!IsInternal)
                {
                    if (!File.Exists(minGWPath + "\\bin\\ConsolePauser.exe")) CompileConsolePauser();
                    JArray keybindJson = GetKeyboardBindingJson();
                    string keybindpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Code\\User\\keybindings.json";
                    Logging.Log($"Writing keybindings.json to user settings folder {keybindpath}");
                    StreamWriter keybindsw = new StreamWriter(keybindpath);
                    keybindsw.Write(keybindJson.ToString());
                    keybindsw.Flush();
                    keybindsw.Close();
                    Logging.Log("keybindings.json written. Contents:");
                    Logging.Log(keybindJson.ToString(), LogType.Multiline);
                }
                Logging.Log("Create .vscode folder and hide...");
                Directory.CreateDirectory(workspacePath + "\\.vscode");
                File.SetAttributes(workspacePath + "\\.vscode", FileAttributes.Hidden);


                Logging.Log("Writing " + workspacePath + "\\.vscode\\launch.json");
                StreamWriter launchsw = new StreamWriter(workspacePath + "\\.vscode\\launch.json");
                launchsw.Write(launchJson.ToString());
                launchsw.Flush();
                launchsw.Close();
                Logging.Log("launch.json written. Contents:");
                Logging.Log(launchJson.ToString(), LogType.Multiline);


                Logging.Log("Writing " + workspacePath + "\\.vscode\\tasks.json");
                StreamWriter taskssw = new StreamWriter(workspacePath + "\\.vscode\\tasks.json");
                taskssw.Write(tasksJson.ToString());
                taskssw.Flush();
                taskssw.Close();
                Logging.Log("tasks.json written. Contents:");
                Logging.Log(tasksJson.ToString(), LogType.Multiline);


                Logging.Log("Writing " + workspacePath + "\\.vscode\\settings.json");
                StreamWriter settingssw = new StreamWriter(workspacePath + "\\.vscode\\settings.json");
                settingssw.Write(settingsJson.ToString());
                settingssw.Flush();
                settingssw.Close();
                Logging.Log("settings.json written. Contents:");
                Logging.Log(settingsJson.ToString(), LogType.Multiline);


                isSuccess = true;
                Logging.Log("All JSON written. Configure almost done.");
                tabControlMain.SelectedIndex++;
            }
            catch (Exception ex)
            {
                Logging.Log("Catch exception. Aborted: " + ex.Message);
                MessageBox.Show("配置时发生错误：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool CreateDesktopShortcut()
        {
            Logging.Log("Creating desktop shortcut...");
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Visual Studio Code.lnk";
                if (File.Exists(desktop))
                {
                    File.Delete(desktop);
                    Logging.Log("Removed existed shortcut.");
                }
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(desktop);
                shortcut.TargetPath = vsCodePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(vsCodePath);
                shortcut.WindowStyle = 1; // normal size
                shortcut.Description = "Visual Studio Code";
                shortcut.IconLocation = vsCodePath;
                shortcut.Arguments = $"\"{workspacePath}\"";
                shortcut.Save();
                Logging.Log($"Shortcut created, links to \"{shortcut.TargetPath}\" {shortcut.Arguments}");
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log($"Failed while creating shortcut. Exception: " + ex.Message);
                return false;
            }
        }

        private void buttonFinishAll_Click(object sender, EventArgs e)
        {
            buttonFinishAll.Text = "请稍候…";
            buttonFinishAll.Enabled = false;
            Thread hitting = null;
            if (checkBoxHitCount.Checked)
            {
                hitting = new Thread(new ThreadStart(() => { FormSettings.HitCount(); }));
                hitting.Start();
            }
            if (checkBoxGenTest.Checked)
            {
                string filepath = GenerateTestFile(workspacePath);
                if (checkBoxOpen.Checked) LoadVSCode(workspacePath, filepath);
            }
            else if (checkBoxOpen.Checked) LoadVSCode(workspacePath);
            if (checkBoxDesktopShortcut.Checked) CreateDesktopShortcut();
            if (checkBoxHitCount.Checked)
                hitting.Join();
            Close();
        }


        /// <summary>
        /// 绘制左侧“导航”栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelNavigate_Paint(object sender, PaintEventArgs e)
        {
            string[] texts =
            {
                "- 欢迎",
                "- 配置 MinGW",
                "- 安装扩展",
                "- 选择样式",
                "- 完成"
            };
            Point pt;
            int index = tabControlMain.SelectedIndex;
            int height = 20;
            for (int i = 0; i < index; i++)
            {
                pt = new Point(0, i * height);
                TextRenderer.DrawText(e.Graphics, texts[i] + Environment.NewLine, new Font(FontFamily.GenericSerif, 9, FontStyle.Regular), pt, Color.Black);
            }
            pt = new Point(0, index * height);
            TextRenderer.DrawText(e.Graphics, texts[index] + Environment.NewLine, new Font(FontFamily.GenericSerif, 9, FontStyle.Bold), pt, Color.Black);
            for (int i = index + 1; i < 5; i++)
            {
                pt = new Point(0, i * height);
                TextRenderer.DrawText(e.Graphics, texts[i] + Environment.NewLine, new Font(FontFamily.GenericSerif, 9, FontStyle.Regular), pt, Color.Gray);
            }
        }

    }
}
