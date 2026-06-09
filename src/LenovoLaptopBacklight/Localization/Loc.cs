using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace LenovoLaptopBacklight.Localization;

/// <summary>
/// Tiny runtime localization manager. Static UI binds via the {loc:Tr key} markup extension to the
/// indexer; calling SetLanguage raises the indexer PropertyChanged so all bindings refresh live.
/// Dynamic strings call Loc.I["key"] (and string.Format for parameterized entries).
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc I { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _language = "en";
    public string Language => _language;

    public string this[string key] =>
        (_language == "zh" && Zh.TryGetValue(key, out var z)) ? z
        : (En.TryGetValue(key, out var e) ? e : key);

    public void SetLanguage(string code)
    {
        var lang = code == "zh" ? "zh" : "en";
        if (_language == lang) return;
        _language = lang;
        // Refresh every {loc:Tr} binding + every Loc.I[...] consumer.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
    }

    // ---------------- English ----------------
    private static readonly Dictionary<string, string> En = new()
    {
        // window + tabs
        ["window.title"]   = "Laptop Backlight Control for Lenovo",
        ["tab.control"]    = "🔆  Control",
        ["tab.detection"]  = "🔍  Detection",
        ["tab.settings"]   = "🛠  Settings",
        ["tab.about"]      = "ℹ  About",
        ["common.reset"]   = "Reset",
        ["common.errorTitle"] = "Error",
        ["common.errorFmt"]   = "Error: {0}",

        // Control
        ["control.title"]    = "Backlight Control",
        ["control.subtitle"] = "Click a stage to apply it immediately.",
        ["control.current"]  = "Current value: ",
        ["control.refresh"]  = "Refresh",
        ["control.noRegTitle"] = "⚠  No register configured.",
        ["control.noRegBody"]  = "Run the Detection tab to find your keyboard backlight EC register first.",
        ["control.status.registerFmt"] = "Register: 0x{0:X2}",
        ["control.status.notConfigured"] = "Not configured — run Detection first.",
        ["control.status.appliedFmt"]    = "Applied: {0}  (0x{1:X2})",
        ["control.status.mismatchFmt"]   = "Wrote {0} (0x{1:X2}) but register reads 0x{2:X2}",
        ["control.error.title"] = "EC Write Error",

        // Detection
        ["detection.title"]    = "Detection",
        ["detection.subtitle"] = "Automatically discovers the EC register that controls your keyboard backlight.",
        ["detection.howTitle"] = "How it works",
        ["detection.howBody"]  = "Detection reads all 256 Embedded Controller registers in each backlight state you set. It then finds registers that are stable within each state but differ across states — those are your backlight control bytes. You can Skip any level your keyboard doesn't have.",
        ["detection.readonly"] = "⚠  Detection is read-only. Writing only happens during the optional Verify step, using exactly the values captured from your hardware.",
        ["detection.start"]    = "Start Detection",
        ["detection.step1"]    = "Step 1 — Capture each stage",
        ["detection.capture"]  = "Capture",
        ["detection.skip"]     = "Skip this level",
        ["detection.analyse"]  = "Analyse →",
        ["detection.skipped"]  = "  (skipped)",
        ["detection.step2"]    = "Step 2 — Review candidates",
        ["detection.verify"]   = "Verify by cycling stages →",
        ["detection.watch"]    = "WATCH YOUR KEYBOARD",
        ["detection.nowShowing"] = "Now showing:",
        ["detection.step3"]    = "Step 3 — Confirm",
        ["detection.confirmQuestion"] = "Did the keyboard backlight correctly follow each stage during the verify cycle?",
        ["detection.saveYes"]  = "✔ Yes — Save Configuration",
        ["detection.resetNo"]  = "✗ No — Reset Detection",
        ["detection.savedTitle"] = "✔  Configuration saved!",
        ["detection.savedBody"]  = "Your keyboard backlight EC register has been detected and saved. Switch to the Control tab to apply stages immediately, or use Settings → \"Apply on restart\" to set a stage automatically at startup.",
        ["detection.runAgain"] = "Run Detection Again",
        // Detection dynamic
        ["wiz.welcome"] = "Welcome to Detection.",
        ["wiz.driverErrorFmt"] = "Cannot open EC driver:\n{0}\n\nMake sure you are running as Administrator and that Memory Integrity (Core Isolation) is turned OFF.",
        ["wiz.driverErrorTitle"] = "Driver Error",
        ["wiz.capturingFmt"] = "Capturing {0}…",
        ["wiz.capturedFmt"]  = "Captured: {0}",
        ["wiz.skippedFmt"]   = "Skipped: {0}",
        ["wiz.instrSetFmt"]  = "Set your keyboard backlight to \"{0}\" using Fn+Space, then click Capture.\nIf your keyboard has no \"{0}\" level, click Skip.",
        ["wiz.instrAllResolved"] = "All stages resolved. Click Analyse to find the register.",
        ["wiz.instrNeedTwo"]     = "You need at least 2 captured stages to detect the register. Click Reset to start over.",
        ["wiz.analysing"] = "Analysing…",
        ["wiz.bestCandidateFmt"] = "Best candidate: register 0x{0:X2} — {1} distinct values across {2} captured stages.",
        ["wiz.noCandidate"] = "No clean candidate found. Make sure each backlight level was clearly different, then Reset and try again.",
        ["wiz.candidateRowFmt"] = "Register 0x{0:X2}  [{1}]  ({2} distinct values)",
        ["wiz.verifyWatch"] = "Verifying — watch your keyboard backlight.",
        ["wiz.noCandidatesVerify"] = "No candidates to verify.",
        ["wiz.verifyStepFmt"] = "Step {0} of {1}  —  writing 0x{2:X2} to register 0x{3:X2}",
        ["wiz.verifyDone"] = "Done",
        ["wiz.verifyDoneDetail"] = "Did the backlight follow each stage?",
        ["wiz.verifyFinished"] = "Verification finished.",
        ["wiz.verifyErrorFmt"] = "Error during verify: {0}",
        ["wiz.savedOk"] = "Configuration saved!",

        // Settings
        ["settings.title"] = "Settings",
        ["settings.language"] = "Language",
        ["settings.applyOnRestart"] = "Apply on restart",
        ["settings.applyOnRestartDesc"] = "Automatically set the backlight to a chosen stage every time the computer starts.",
        ["settings.onRestartSet"] = "On restart, set backlight to:",
        ["settings.defenderAllow"] = "Allow driver (add Defender exclusion)",
        ["settings.defenderRemove"] = "Remove exclusion",
        ["settings.defenderNote"] = "WinRing0 is a known-flagged driver; Windows Defender blocks it by default. The exclusion lets it load. This is required for the backlight driver to work reliably.",
        ["settings.ecAdvanced"] = "EC Register (Advanced)",
        ["settings.ecAdvancedNote"] = "These are filled automatically by Detection. Only change manually if you know your register. Enter as decimal (e.g. 174 for 0xAE). Use -1 to disable the mirror register.",
        ["settings.primaryReg"] = "Primary register (decimal):",
        ["settings.mirrorReg"] = "Mirror register (decimal):",
        ["settings.stages"] = "Backlight Stages",
        ["settings.colName"] = "Name",
        ["settings.colValue"] = "Value (decimal)",
        ["settings.addStage"] = "Add Stage",
        ["settings.saveChanges"] = "Save Changes",
        ["settings.resetDefaults"] = "Reset to Defaults",
        ["settings.openLog"] = "Open Log File",
        ["settings.openConfig"] = "Open Config File",
        // Settings dynamic
        ["set.configPathFmt"] = "Config: {0}",
        ["set.runDetectionFirst"] = "Run Detection first — no register configured.",
        ["set.onRestartSetFmt"] = "On restart, the backlight will be set to \"{0}\".",
        ["set.restartOff"] = "Restart action turned off.",
        ["set.taskErrorTitle"] = "Scheduled Task Error",
        ["set.defenderAllowed"] = "Windows Defender: driver is allowed (exclusion present).",
        ["set.defenderNone"] = "Windows Defender: no exclusion — the driver may be blocked on a fresh start.",
        ["set.saved"] = "Saved.",
        ["set.stageExists"] = "A stage with that name already exists.",
        ["set.stageAdded"] = "Stage added.",
        ["set.stageDeletedFmt"] = "Stage '{0}' deleted.",
        ["set.resetStagesConfirm"] = "Reset stages to Auto/Bright/Dim/Off defaults?\nRegister addresses are preserved.",
        ["set.resetStagesTitle"] = "Reset Stages",
        ["set.stagesReset"] = "Stages reset to defaults.",

        // About
        ["about.appName"] = "Laptop Backlight Control for Lenovo",
        ["about.versionFmt"] = "Version {0}",
        ["about.author"] = "Author",
        ["about.authorLine"] = "Author: mcyikhei",
        ["about.github"] = "GitHub: ",
        ["about.copy"] = "Copy",
        ["about.license"] = "MIT License",
        ["about.disclaimerTitle"] = "⚠  Disclaimer",
        ["about.creditsTitle"] = "Credits & Third-Party Software",
        ["about.disclaimer"] =
            "DISCLAIMER: This is an independent, third-party utility and is NOT affiliated with, " +
            "endorsed by, or associated with Lenovo Group Ltd. or any of its subsidiaries. " +
            "\"Lenovo\" is a registered trademark of Lenovo Group Ltd.\n\n" +
            "This software directly accesses low-level hardware (the Embedded Controller) via a " +
            "kernel driver. Use at your own risk. Always back up your data before use. " +
            "The author accepts no liability for any damage, data loss, or warranty issues " +
            "that may arise from using this software.",
        ["about.credits"] =
            "This software builds on the following open-source work:\n\n" +
            "• WinRing0 (OpenLibSys) — kernel-mode I/O port access driver\n" +
            "  License: BSD (see THIRD-PARTY-NOTICES.md)\n\n" +
            "• NoteBook FanControl (hirschmann/nbfc) — ec-probe tool and EC access approach\n" +
            "  License: GPL-3.0\n\n" +
            "• ACPI Embedded Controller Specification (ACPI §12.9)\n" +
            "  The EC read/write protocol implemented here follows the ACPI specification.",

        // Services
        ["def.added"]  = "Driver allowed in Windows Defender (exclusion added).",
        ["def.addFailFmt"] = "Could not add Defender exclusion: {0}",
        ["def.removed"] = "Defender exclusion removed.",
        ["def.removeFailFmt"] = "Could not remove Defender exclusion: {0}",
        ["drv.defenderBlocked"] = "Windows Defender is blocking the WinRing0 driver (it flags it as a vulnerable driver). In Settings, click \"Allow driver (add Defender exclusion)\", then try again.",
        ["boot.createFailFmt"] = "Could not register the scheduled task: {0}",
        ["boot.runFailFmt"] = "Could not run the scheduled task: {0}",
    };

    // ---------------- 简体中文 ----------------
    private static readonly Dictionary<string, string> Zh = new()
    {
        ["window.title"]   = "联想笔记本背光控制",
        ["tab.control"]    = "🔆  控制",
        ["tab.detection"]  = "🔍  检测",
        ["tab.settings"]   = "🛠  设置",
        ["tab.about"]      = "ℹ  关于",
        ["common.reset"]   = "重置",
        ["common.errorTitle"] = "错误",
        ["common.errorFmt"]   = "错误：{0}",

        ["control.title"]    = "背光控制",
        ["control.subtitle"] = "点击某个档位即可立即应用。",
        ["control.current"]  = "当前值：",
        ["control.refresh"]  = "刷新",
        ["control.noRegTitle"] = "⚠  未配置寄存器。",
        ["control.noRegBody"]  = "请先在“检测”选项卡中找到键盘背光的 EC 寄存器。",
        ["control.status.registerFmt"] = "寄存器：0x{0:X2}",
        ["control.status.notConfigured"] = "未配置 — 请先运行检测。",
        ["control.status.appliedFmt"]    = "已应用：{0}（0x{1:X2}）",
        ["control.status.mismatchFmt"]   = "已写入 {0}（0x{1:X2}），但寄存器读回 0x{2:X2}",
        ["control.error.title"] = "EC 写入错误",

        ["detection.title"]    = "检测",
        ["detection.subtitle"] = "自动找出控制键盘背光的 EC 寄存器。",
        ["detection.howTitle"] = "工作原理",
        ["detection.howBody"]  = "检测会在你设置的每个背光状态下读取全部 256 个嵌入式控制器（EC）寄存器，然后找出在每个状态内保持稳定、但在不同状态间存在差异的寄存器 —— 它们就是控制背光的字节。键盘没有的档位可以跳过。",
        ["detection.readonly"] = "⚠  检测过程为只读。仅在可选的“验证”步骤才会写入，且只使用从你的硬件采集到的值。",
        ["detection.start"]    = "开始检测",
        ["detection.step1"]    = "第 1 步 — 采集每个档位",
        ["detection.capture"]  = "采集",
        ["detection.skip"]     = "跳过此档位",
        ["detection.analyse"]  = "分析 →",
        ["detection.skipped"]  = "（已跳过）",
        ["detection.step2"]    = "第 2 步 — 查看候选寄存器",
        ["detection.verify"]   = "循环切换档位以验证 →",
        ["detection.watch"]    = "请注意观察键盘",
        ["detection.nowShowing"] = "当前显示：",
        ["detection.step3"]    = "第 3 步 — 确认",
        ["detection.confirmQuestion"] = "在验证循环中，键盘背光是否正确地跟随了每个档位？",
        ["detection.saveYes"]  = "✔ 是 — 保存配置",
        ["detection.resetNo"]  = "✗ 否 — 重置检测",
        ["detection.savedTitle"] = "✔  配置已保存！",
        ["detection.savedBody"]  = "已检测并保存键盘背光的 EC 寄存器。切换到“控制”选项卡可立即应用各档位，或在“设置 → 开机时应用”中设置开机自动应用某个档位。",
        ["detection.runAgain"] = "再次运行检测",
        ["wiz.welcome"] = "欢迎使用检测。",
        ["wiz.driverErrorFmt"] = "无法打开 EC 驱动：\n{0}\n\n请确认以管理员身份运行，并已关闭“内存完整性”（核心隔离）。",
        ["wiz.driverErrorTitle"] = "驱动错误",
        ["wiz.capturingFmt"] = "正在采集 {0}…",
        ["wiz.capturedFmt"]  = "已采集：{0}",
        ["wiz.skippedFmt"]   = "已跳过：{0}",
        ["wiz.instrSetFmt"]  = "请用 Fn+Space 将键盘背光设为“{0}”，然后点击“采集”。\n如果你的键盘没有“{0}”档位，请点击“跳过”。",
        ["wiz.instrAllResolved"] = "所有档位均已处理。点击“分析”以查找寄存器。",
        ["wiz.instrNeedTwo"]     = "至少需要采集 2 个档位才能检测寄存器。请点击“重置”重新开始。",
        ["wiz.analysing"] = "正在分析…",
        ["wiz.bestCandidateFmt"] = "最佳候选：寄存器 0x{0:X2} —— 在 {2} 个已采集档位中有 {1} 个不同值。",
        ["wiz.noCandidate"] = "未找到明确的候选项。请确保每个背光档位明显不同，然后“重置”重试。",
        ["wiz.candidateRowFmt"] = "寄存器 0x{0:X2}  [{1}]  （{2} 个不同值）",
        ["wiz.verifyWatch"] = "正在验证 —— 请观察键盘背光。",
        ["wiz.noCandidatesVerify"] = "没有可验证的候选项。",
        ["wiz.verifyStepFmt"] = "第 {0}/{1} 步 —— 正在向寄存器 0x{3:X2} 写入 0x{2:X2}",
        ["wiz.verifyDone"] = "完成",
        ["wiz.verifyDoneDetail"] = "背光是否跟随了每个档位？",
        ["wiz.verifyFinished"] = "验证完成。",
        ["wiz.verifyErrorFmt"] = "验证出错：{0}",
        ["wiz.savedOk"] = "配置已保存！",

        ["settings.title"] = "设置",
        ["settings.language"] = "语言",
        ["settings.applyOnRestart"] = "开机时应用",
        ["settings.applyOnRestartDesc"] = "每次开机时自动将背光设为所选档位。",
        ["settings.onRestartSet"] = "开机后将背光设为：",
        ["settings.defenderAllow"] = "允许驱动（添加 Defender 排除项）",
        ["settings.defenderRemove"] = "移除排除项",
        ["settings.defenderNote"] = "WinRing0 是一个被普遍标记的驱动，Windows Defender 默认会拦截它。添加排除项可让其加载。背光驱动要稳定工作需要此项。",
        ["settings.ecAdvanced"] = "EC 寄存器（高级）",
        ["settings.ecAdvancedNote"] = "这些值由检测自动填写。仅在你确知自己的寄存器时才手动修改。请输入十进制（例如 174 表示 0xAE）。镜像寄存器填 -1 表示禁用。",
        ["settings.primaryReg"] = "主寄存器（十进制）：",
        ["settings.mirrorReg"] = "镜像寄存器（十进制）：",
        ["settings.stages"] = "背光档位",
        ["settings.colName"] = "名称",
        ["settings.colValue"] = "值（十进制）",
        ["settings.addStage"] = "添加档位",
        ["settings.saveChanges"] = "保存更改",
        ["settings.resetDefaults"] = "恢复默认",
        ["settings.openLog"] = "打开日志文件",
        ["settings.openConfig"] = "打开配置文件",
        ["set.configPathFmt"] = "配置：{0}",
        ["set.runDetectionFirst"] = "请先运行检测 —— 未配置寄存器。",
        ["set.onRestartSetFmt"] = "开机后背光将被设为“{0}”。",
        ["set.restartOff"] = "已关闭开机应用。",
        ["set.taskErrorTitle"] = "计划任务错误",
        ["set.defenderAllowed"] = "Windows Defender：驱动已被允许（已存在排除项）。",
        ["set.defenderNone"] = "Windows Defender：无排除项 —— 全新启动时驱动可能被拦截。",
        ["set.saved"] = "已保存。",
        ["set.stageExists"] = "已存在同名档位。",
        ["set.stageAdded"] = "档位已添加。",
        ["set.stageDeletedFmt"] = "档位“{0}”已删除。",
        ["set.resetStagesConfirm"] = "将档位恢复为 Auto/Bright/Dim/Off 默认值？\n寄存器地址会保留。",
        ["set.resetStagesTitle"] = "重置档位",
        ["set.stagesReset"] = "档位已恢复默认。",

        ["about.appName"] = "联想笔记本背光控制",
        ["about.versionFmt"] = "版本 {0}",
        ["about.author"] = "作者",
        ["about.authorLine"] = "作者：mcyikhei",
        ["about.github"] = "GitHub：",
        ["about.copy"] = "复制",
        ["about.license"] = "MIT 许可证",
        ["about.disclaimerTitle"] = "⚠  免责声明",
        ["about.creditsTitle"] = "致谢与第三方软件",
        ["about.disclaimer"] =
            "免责声明：本软件为独立的第三方工具，与联想集团有限公司及其任何子公司无任何隶属、" +
            "认可或关联关系。“Lenovo（联想）”是联想集团有限公司的注册商标。\n\n" +
            "本软件通过内核驱动直接访问底层硬件（嵌入式控制器）。请自行承担使用风险，" +
            "使用前请务必备份数据。对于因使用本软件而导致的任何损坏、数据丢失或保修问题，" +
            "作者概不负责。",
        ["about.credits"] =
            "本软件基于以下开源项目构建：\n\n" +
            "• WinRing0 (OpenLibSys) —— 内核态 I/O 端口访问驱动\n" +
            "  许可证：BSD（详见 THIRD-PARTY-NOTICES.md）\n\n" +
            "• NoteBook FanControl (hirschmann/nbfc) —— ec-probe 工具与 EC 访问方法\n" +
            "  许可证：GPL-3.0\n\n" +
            "• ACPI 嵌入式控制器规范（ACPI §12.9）\n" +
            "  本软件实现的 EC 读写协议遵循 ACPI 规范。",

        ["def.added"]  = "已在 Windows Defender 中允许驱动（已添加排除项）。",
        ["def.addFailFmt"] = "无法添加 Defender 排除项：{0}",
        ["def.removed"] = "已移除 Defender 排除项。",
        ["def.removeFailFmt"] = "无法移除 Defender 排除项：{0}",
        ["drv.defenderBlocked"] = "Windows Defender 正在拦截 WinRing0 驱动（将其标记为易受攻击的驱动）。请在“设置”中点击“允许驱动（添加 Defender 排除项）”，然后重试。",
        ["boot.createFailFmt"] = "无法注册计划任务：{0}",
        ["boot.runFailFmt"] = "无法运行计划任务：{0}",
    };
}
