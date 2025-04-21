using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatClient.Tool.HelperInterface;

namespace ChatClient.BaseService.Helper.Linux;

internal class LinuxFileDialog : ISystemFileDialog
{
    public async Task<string> OpenFileAsync(IntPtr ownerHandle, string windowName = "Open File",
        string initialDir = "",
        string[]? fileTypes = null)
    {
        var arguments = new StringBuilder("--file-selection");

        // 添加标题
        arguments.Append($" --title=\"{windowName}\"");

        // 添加初始目录
        if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
        {
            arguments.Append($" --filename=\"{initialDir}/\"");
        }

        // 添加文件过滤器
        if (fileTypes != null && fileTypes.Length > 0)
        {
            var filters = BuildFilterString(fileTypes);
            if (!string.IsNullOrEmpty(filters))
            {
                arguments.Append($" --file-filter=\"{filters}\"");
            }
        }

        // 执行Zenity命令
        return await ExecuteZenityCommand(arguments.ToString());
    }

    public async Task<string> SaveFileAsync(IntPtr ownerHandle, string defaultFileName = "",
        string windowName = "Save File",
        string initialDir = "",
        string[]? fileTypes = null)
    {
        var arguments = new StringBuilder("--file-selection --save");

        // 添加标题
        arguments.Append($" --title=\"{windowName}\"");

        // 添加初始目录和默认文件名
        if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
        {
            if (!string.IsNullOrEmpty(defaultFileName))
            {
                arguments.Append($" --filename=\"{Path.Combine(initialDir, defaultFileName)}\"");
            }
            else
            {
                arguments.Append($" --filename=\"{initialDir}/\"");
            }
        }
        else if (!string.IsNullOrEmpty(defaultFileName))
        {
            arguments.Append($" --filename=\"{defaultFileName}\"");
        }

        // 添加文件过滤器
        if (fileTypes != null && fileTypes.Length > 0)
        {
            var filters = BuildFilterString(fileTypes);
            if (!string.IsNullOrEmpty(filters))
            {
                arguments.Append($" --file-filter=\"{filters}\"");
            }
        }

        // 确认覆盖提示通过Zenity本身的--confirm-overwrite选项实现
        arguments.Append(" --confirm-overwrite");

        // 执行Zenity命令
        return await ExecuteZenityCommand(arguments.ToString());
    }

    private string BuildFilterString(string[]? fileTypes)
    {
        if (fileTypes == null || fileTypes.Length == 0)
        {
            return string.Empty;
        }

        // 检查是否为成对的描述和文件类型格式
        if (fileTypes.Length % 2 != 0)
        {
            throw new ArgumentException("fileTypes数组应该包含成对的描述和扩展名模式");
        }

        var filters = new StringBuilder();

        for (int i = 0; i < fileTypes.Length; i += 2)
        {
            string description = fileTypes[i];
            string extensions = fileTypes[i + 1].Replace("*.", "*."); // 确保扩展名格式正确

            if (i > 0) filters.Append(" ");

            // Zenity中的格式为 "Description | pattern1 pattern2"
            filters.Append($"{description} | {extensions.Replace(";", " ")}");
        }

        // 添加"所有文件"选项
        if (fileTypes.Length > 0)
        {
            filters.Append(" All Files | *.*");
        }

        return filters.ToString();
    }

    private async Task<string> ExecuteZenityCommand(string arguments)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "zenity",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            // 异步读取输出
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // 如果用户取消了对话框，Zenity会返回非零退出代码
            if (process.ExitCode != 0)
            {
                return null;
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"执行Zenity命令时出错: {ex.Message}");
            return null;
        }
    }
}