using System.Collections.Generic;
using System.Globalization;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XavWelcomeScreen", "Xavlios", "1.0.0")]
    [Description("WelcomeScreen — sidebar-nav welcome screen")]
    public class XavWelcomeScreen : RustPlugin
    {
        #region Fields

        private Configuration _config;

        private const string UI_ROOT    = "WelcomeScreen_Root";
        private const string UI_PANEL   = "WS_Panel";
        private const string UI_SIDEBAR = "WS_Sidebar";
        private const string UI_CZONE   = "WS_CZone";
        private const string UI_CONTENT = "WS_Content";
        private const string CMD_CLOSE  = "xavlios.welcomescreen.close";
        private const string CMD_TAB    = "xavlios.welcomescreen.tab";
        private const string PERM_SKIP  = "xavlioswelcomescreen.bypass";

        private static readonly string[] TabLabels = { "GENERAL", "SERVER RULES", "COMMANDS" };

        #endregion

        #region Configuration

        private class Configuration
        {
            public bool   Enabled    = true;
            public float  ShowDelay  = 2f;
            public int    MaxPlayers = 100;

            public string ServerName = "SERVER NAME";
            public string Tagline    = "1.5x Modded  •  Monthly Wipe  •  Active Community";
            public string DiscordUrl = "discord.gg/XXXXXXXXX";
            public string WebsiteUrl = "www.google.com";

            public List<StatCard> StatCards = new List<StatCard>
            {
                new StatCard { Label = "WIPE SCHEDULE", Value = "First Thursday",  Sub = "BP: Forced Wipes Only",  Accent = "0.0 0.75 0.85 1.0"  },
                new StatCard { Label = "GATHER RATE",   Value = "1.5x",            Sub = "Balanced Progression",  Accent = "0.91 0.71 0.13 1.0" },
                new StatCard { Label = "TEAM SIZE",     Value = "Max 6",           Sub = "Allying Encouraged",    Accent = "0.91 0.71 0.13 1.0" }
            };

            public List<string> Features = new List<string>
            {
                "1.5x Gather Rates",
                "Monthly Map Wipe",
                "Active Admins & Anti-Cheat",
                "Offline Raid Protection",
                "Custom Skill Tree",
                "Daily Rewards System",
                "DDoS Protected",
                "Lag Free Experience"
            };

            public List<string> Rules = new List<string>
            {
                "<color=#FF4545>No</color> cheating in any shape or form.",
                "<color=#FF4545>No</color> toxicity. Personal attacks and discrimination <color=#FF4545>will not</color> be tolerated.",
                "<color=#FF4545>Do not</color> advertise other servers.",
                "<color=#FF4545>No</color> text spamming. English only in chat.",
                "<color=#FF4545>No</color> VAC bans under 90 days allowed.",
                "<color=#E8B422>Max</color> team size is 6. Allying is <color=#5AE870>allowed and encouraged</color>.",
                "Old teammates <color=#E8B422>must be cleared</color> from TC for the remainder of the wipe.",
                "<color=#E8B422>Evading</color> offline protection is <color=#FF4545>not allowed</color> and is considered cheating."
            };

            public List<CommandEntry> Commands = new List<CommandEntry>
            {
                new CommandEntry { Cmd = "/help | /info",          Desc = "Open this menu" },
                new CommandEntry { Cmd = "/daily",                 Desc = "Open daily rewards" },
                new CommandEntry { Cmd = "/pm <player> <msg>",     Desc = "Private message someone" },
                new CommandEntry { Cmd = "/noskin",                Desc = "Toggle skins on other players" },
                new CommandEntry { Cmd = "/playtime",              Desc = "Check your total playtime" },
                new CommandEntry { Cmd = "/playtime top",          Desc = "Top players by playtime" },
                new CommandEntry { Cmd = "/st | /skilltree",       Desc = "Open the skill tree" },
                new CommandEntry { Cmd = "pop | !pop",             Desc = "Show online player count" }
            };

            public ColorTheme Colors = new ColorTheme();
        }

        private class StatCard
        {
            public string Label;
            public string Value;
            public string Sub;
            public string Accent = "0.0 0.75 0.85 1.0";
        }

        private class CommandEntry
        {
            public string Cmd;
            public string Desc;
        }

        private class ColorTheme
        {
            public string Overlay       = "0 0 0 0.70";
            public string Panel         = "0.07 0.075 0.095 0.98";
            public string SidebarBg     = "0.045 0.050 0.065 1.0";
            public string SidebarDiv    = "0.12 0.13 0.17 1.0";
            public string TitleBarBg    = "0.05 0.055 0.072 1.0";
            public string CardBg        = "0.10 0.11 0.145 1.0";
            public string NavActiveBg   = "0.0 0.75 0.85 0.10";
            public string Accent        = "0.0 0.75 0.85 1.0";
            public string Gold          = "0.91 0.71 0.13 1.0";
            public string TextPrimary   = "0.93 0.94 0.96 1.0";
            public string TextSecondary = "0.48 0.52 0.58 1.0";
            public string CloseBtn      = "0.70 0.18 0.18 1.0";
            public string RuleNumColor  = "0.0 0.75 0.85 1.0";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Config error — loading defaults.");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();
        protected override void SaveConfig()        => Config.WriteObject(_config);

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(PERM_SKIP, this);
            cmd.AddConsoleCommand(CMD_CLOSE, this, nameof(CcClose));
            cmd.AddConsoleCommand(CMD_TAB,   this, nameof(CcTab));
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_config.Enabled || player == null) return;
            if (permission.UserHasPermission(player.UserIDString, PERM_SKIP)) return;
            timer.Once(_config.ShowDelay, () =>
            {
                if (player == null || !player.IsConnected) return;
                ShowUI(player, 0);
            });
        }

        private void Unload()
        {
            foreach (var p in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(p, UI_ROOT);
        }

        [ChatCommand("welcome")]
        private void CmdWelcome(BasePlayer p, string c, string[] a) => ShowUI(p, 0);
        [ChatCommand("info")]
        private void CmdInfo(BasePlayer p, string c, string[] a)    => ShowUI(p, 0);

        private void CcClose(ConsoleSystem.Arg arg) { var p = arg.Player(); if (p != null) CuiHelper.DestroyUi(p, UI_ROOT); }
        private void CcTab(ConsoleSystem.Arg arg)   { var p = arg.Player(); if (p != null) ShowUI(p, arg.GetInt(0, 0)); }

        #endregion

        #region UI — Shell

        private void ShowUI(BasePlayer player, int activeTab)
        {
            CuiHelper.DestroyUi(player, UI_ROOT);
            var c   = new CuiElementContainer();
            var col = _config.Colors;

            c.Add(new CuiPanel
            {
                Image         = { Color = col.Overlay },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true
            }, "Overlay", UI_ROOT);

            c.Add(new CuiPanel
            {
                Image         = { Color = col.Panel },
                RectTransform = { AnchorMin = "0.12 0.07", AnchorMax = "0.90 0.93" }
            }, UI_ROOT, UI_PANEL);

            BuildSidebar(c, col, activeTab);
            BuildContentZone(c, col, activeTab, player.displayName);

            CuiHelper.AddUi(player, c);
        }

        #endregion

        #region UI — Sidebar

        private void BuildSidebar(CuiElementContainer c, ColorTheme col, int activeTab)
        {
            c.Add(new CuiPanel
            {
                Image         = { Color = col.SidebarBg },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.220 1" }
            }, UI_PANEL, UI_SIDEBAR);

            c.Add(new CuiPanel
            {
                Image         = { Color = col.Accent },
                RectTransform = { AnchorMin = "0.980 0.02", AnchorMax = "1.000 0.98" }
            }, UI_SIDEBAR);

            c.Add(new CuiLabel
            {
                Text          = { Text = $"<b>{_config.ServerName.ToUpper()}</b>", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = col.Accent, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.05 0.865", AnchorMax = "0.94 0.975" }
            }, UI_SIDEBAR);

            if (!string.IsNullOrWhiteSpace(_config.Tagline))
            {
                c.Add(new CuiLabel
                {
                    Text          = { Text = _config.Tagline, FontSize = 8, Align = TextAnchor.MiddleCenter, Color = col.TextSecondary },
                    RectTransform = { AnchorMin = "0.05 0.820", AnchorMax = "0.94 0.868" }
                }, UI_SIDEBAR);
            }

            c.Add(new CuiPanel
            {
                Image         = { Color = col.CardBg },
                RectTransform = { AnchorMin = "0.07 0.755", AnchorMax = "0.91 0.808" }
            }, UI_SIDEBAR, "WS_Online");

            int online = BasePlayer.activePlayerList.Count;
            c.Add(new CuiLabel
            {
                Text          = { Text = $"<color=#{Hex(col.Accent)}>ONLINE</color>   <b>{online}</b> <color=#{Hex(col.TextSecondary)}>/ {_config.MaxPlayers}</color>", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = col.TextPrimary },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "WS_Online");

            SbDivider(c, col, 0.748f);

            const float navTop = 0.740f;
            const float navH   = 0.125f;

            for (int i = 0; i < TabLabels.Length; i++)
            {
                float yMax   = navTop - i * navH;
                float yMin   = yMax - navH + 0.003f;
                bool  active = i == activeTab;

                string btn = c.Add(new CuiButton
                {
                    Button        = { Color = active ? col.NavActiveBg : "0 0 0 0", Command = $"{CMD_TAB} {i}" },
                    RectTransform = { AnchorMin = $"0 {yMin:F3}", AnchorMax = $"0.979 {yMax:F3}" },
                    Text          = { Text = TabLabels[i], FontSize = 11, Align = TextAnchor.MiddleCenter, Color = active ? col.TextPrimary : col.TextSecondary, Font = active ? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf" }
                }, UI_SIDEBAR);

                if (active)
                {
                    c.Add(new CuiPanel
                    {
                        Image         = { Color = col.Accent },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0.028 1" }
                    }, btn);
                }
            }

            float belowNav = navTop - TabLabels.Length * navH - 0.004f;
            SbDivider(c, col, belowNav);

            float sy = belowNav - 0.012f;

            c.Add(new CuiLabel
            {
                Text          = { Text = "COMMUNITY", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = col.TextSecondary },
                RectTransform = { AnchorMin = $"0.10 {sy - 0.032f:F3}", AnchorMax = $"0.92 {sy:F3}" }
            }, UI_SIDEBAR);
            sy -= 0.044f;

            c.Add(new CuiLabel
            {
                Text          = { Text = "<b>Discord</b>", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = col.TextPrimary },
                RectTransform = { AnchorMin = $"0.10 {sy - 0.040f:F3}", AnchorMax = $"0.92 {sy:F3}" }
            }, UI_SIDEBAR);
            sy -= 0.042f;

            c.Add(new CuiLabel
            {
                Text          = { Text = _config.DiscordUrl, FontSize = 9, Align = TextAnchor.MiddleLeft, Color = col.TextSecondary },
                RectTransform = { AnchorMin = $"0.10 {sy - 0.033f:F3}", AnchorMax = $"0.92 {sy:F3}" }
            }, UI_SIDEBAR);
            sy -= 0.050f;

            c.Add(new CuiLabel
            {
                Text          = { Text = "<b>Website</b>", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = col.TextPrimary },
                RectTransform = { AnchorMin = $"0.10 {sy - 0.040f:F3}", AnchorMax = $"0.92 {sy:F3}" }
            }, UI_SIDEBAR);
            sy -= 0.042f;

            c.Add(new CuiLabel
            {
                Text          = { Text = _config.WebsiteUrl, FontSize = 9, Align = TextAnchor.MiddleLeft, Color = col.TextSecondary },
                RectTransform = { AnchorMin = $"0.10 {sy - 0.033f:F3}", AnchorMax = $"0.92 {sy:F3}" }
            }, UI_SIDEBAR);
        }

        private void SbDivider(CuiElementContainer c, ColorTheme col, float y)
        {
            c.Add(new CuiPanel
            {
                Image         = { Color = col.SidebarDiv },
                RectTransform = { AnchorMin = $"0.06 {y:F3}", AnchorMax = $"0.92 {y + 0.003f:F3}" }
            }, UI_SIDEBAR);
        }

        #endregion

        #region UI — Content Zone

        private void BuildContentZone(CuiElementContainer c, ColorTheme col, int tab, string playerName)
        {
            c.Add(new CuiPanel
            {
                Image         = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.220 0", AnchorMax = "1 1" }
            }, UI_PANEL, UI_CZONE);

            c.Add(new CuiPanel
            {
                Image         = { Color = col.TitleBarBg },
                RectTransform = { AnchorMin = "0 0.918", AnchorMax = "1 1" }
            }, UI_CZONE, "WS_TitleBar");

            c.Add(new CuiLabel
            {
                Text          = { Text = $"<b>{TabLabels[tab]}</b>", FontSize = 17, Align = TextAnchor.MiddleLeft, Color = col.TextPrimary, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.03 0", AnchorMax = "0.85 1" }
            }, "WS_TitleBar");

            c.Add(new CuiButton
            {
                Button        = { Color = col.CloseBtn, Command = CMD_CLOSE },
                RectTransform = { AnchorMin = "0.942 0.12", AnchorMax = "0.998 0.88" },
                Text          = { Text = "✕", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, "WS_TitleBar");

            c.Add(new CuiPanel
            {
                Image         = { Color = col.Accent },
                RectTransform = { AnchorMin = "0 0.913", AnchorMax = "1 0.918" }
            }, UI_CZONE);

            c.Add(new CuiPanel
            {
                Image         = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.910" }
            }, UI_CZONE, UI_CONTENT);

            switch (tab)
            {
                case 0: TabGeneral(c, col, playerName); break;
                case 1: TabRules(c, col);               break;
                case 2: TabCommands(c, col);            break;
            }
        }

        #endregion

        #region UI — Tab 0: General

        private void TabGeneral(CuiElementContainer c, ColorTheme col, string playerName)
        {
            c.Add(new CuiLabel
            {
                Text          = { Text = $"Welcome back, <color=#{Hex(col.Accent)}><b>{playerName}</b></color>", FontSize = 15, Align = TextAnchor.MiddleLeft, Color = col.TextPrimary },
                RectTransform = { AnchorMin = "0.03 0.895", AnchorMax = "0.97 0.980" }
            }, UI_CONTENT);

            const float cardY0  = 0.695f;
            const float cardY1  = 0.880f;
            const float cardW   = 0.300f;
            const float cardGap = 0.020f;
            const float cardX0  = 0.030f;

            for (int i = 0; i < _config.StatCards.Count && i < 3; i++)
            {
                var   card = _config.StatCards[i];
                float xMin = cardX0 + i * (cardW + cardGap);
                float xMax = xMin + cardW;

                c.Add(new CuiPanel
                {
                    Image         = { Color = col.CardBg },
                    RectTransform = { AnchorMin = $"{xMin:F3} {cardY0:F3}", AnchorMax = $"{xMax:F3} {cardY1:F3}" }
                }, UI_CONTENT, $"WS_Card{i}");

                c.Add(new CuiPanel
                {
                    Image         = { Color = card.Accent },
                    RectTransform = { AnchorMin = "0 0.880", AnchorMax = "1 1" }
                }, $"WS_Card{i}");

                c.Add(new CuiLabel
                {
                    Text          = { Text = $"<b>{card.Label}</b>", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0.85" },
                    RectTransform = { AnchorMin = "0 0.880", AnchorMax = "1 1" }
                }, $"WS_Card{i}");

                c.Add(new CuiLabel
                {
                    Text          = { Text = $"<b>{card.Value}</b>", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = col.TextPrimary, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = "0.05 0.450", AnchorMax = "0.95 0.875" }
                }, $"WS_Card{i}");

                c.Add(new CuiLabel
                {
                    Text          = { Text = card.Sub, FontSize = 10, Align = TextAnchor.MiddleCenter, Color = col.TextSecondary },
                    RectTransform = { AnchorMin = "0.05 0.060", AnchorMax = "0.95 0.455" }
                }, $"WS_Card{i}");
            }

            c.Add(new CuiLabel
            {
                Text          = { Text = "<b>SERVER FEATURES</b>", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = col.Gold, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.03 0.630", AnchorMax = "0.97 0.688" }
            }, UI_CONTENT);

            c.Add(new CuiPanel
            {
                Image         = { Color = col.Gold },
                RectTransform = { AnchorMin = "0.03 0.624", AnchorMax = "0.97 0.627" }
            }, UI_CONTENT);

            var   feats = _config.Features;
            int   half  = (feats.Count + 1) / 2;
            const float featTop = 0.615f;
            const float featH   = 0.072f;

            for (int i = 0; i < feats.Count; i++)
            {
                bool  right = i >= half;
                float xMin  = right ? 0.510f : 0.030f;
                float xMax  = right ? 0.970f : 0.490f;
                int   row   = right ? i - half : i;
                float y     = featTop - row * featH;
                if (y < 0.02f) break;

                c.Add(new CuiLabel
                {
                    Text          = { Text = $"<color=#{Hex(col.Accent)}>▸</color>  {feats[i]}", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = col.TextPrimary },
                    RectTransform = { AnchorMin = $"{xMin:F3} {y - featH:F3}", AnchorMax = $"{xMax:F3} {y:F3}" }
                }, UI_CONTENT);
            }

            c.Add(new CuiPanel
            {
                Image         = { Color = col.SidebarDiv },
                RectTransform = { AnchorMin = "0.497 0.02", AnchorMax = "0.500 0.622" }
            }, UI_CONTENT);

            c.Add(new CuiLabel
            {
                Text          = { Text = "Type <b>/info</b> in chat to reopen this screen.", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = col.TextSecondary },
                RectTransform = { AnchorMin = "0.03 0.000", AnchorMax = "0.97 0.055" }
            }, UI_CONTENT);
        }

        #endregion

        #region UI — Tab 1: Server Rules

        private void TabRules(CuiElementContainer c, ColorTheme col)
        {
            const float startY = 0.930f;
            const float rowH   = 0.106f;

            for (int i = 0; i < _config.Rules.Count; i++)
            {
                float yMax = startY - i * rowH;
                float yMin = yMax - rowH + 0.004f;
                if (yMin < 0.06f) break;

                c.Add(new CuiLabel
                {
                    Text          = { Text = $"<b>{(i + 1):D2}</b>", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = col.RuleNumColor },
                    RectTransform = { AnchorMin = $"0.030 {yMin:F3}", AnchorMax = $"0.090 {yMax:F3}" }
                }, UI_CONTENT);

                float sepLo = yMin + (yMax - yMin) * 0.15f;
                float sepHi = yMin + (yMax - yMin) * 0.85f;
                c.Add(new CuiPanel
                {
                    Image         = { Color = col.SidebarDiv },
                    RectTransform = { AnchorMin = $"0.093 {sepLo:F3}", AnchorMax = $"0.096 {sepHi:F3}" }
                }, UI_CONTENT);

                c.Add(new CuiLabel
                {
                    Text          = { Text = _config.Rules[i], FontSize = 12, Align = TextAnchor.MiddleLeft, Color = col.TextPrimary },
                    RectTransform = { AnchorMin = $"0.105 {yMin:F3}", AnchorMax = $"0.970 {yMax:F3}" }
                }, UI_CONTENT);
            }

            if (!string.IsNullOrWhiteSpace(_config.DiscordUrl))
            {
                c.Add(new CuiLabel
                {
                    Text          = { Text = $"For up-to-date rules visit our Discord:  <color=#{Hex(col.Accent)}>{_config.DiscordUrl}</color>", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = col.TextSecondary },
                    RectTransform = { AnchorMin = "0.03 0.000", AnchorMax = "0.97 0.055" }
                }, UI_CONTENT);
            }
        }

        #endregion

        #region UI — Tab 2: Commands

        private void TabCommands(CuiElementContainer c, ColorTheme col)
        {
            c.Add(new CuiLabel
            {
                Text          = { Text = "<b>COMMAND</b>", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = col.TextSecondary },
                RectTransform = { AnchorMin = "0.03 0.920", AnchorMax = "0.40 0.970" }
            }, UI_CONTENT);
            c.Add(new CuiLabel
            {
                Text          = { Text = "<b>DESCRIPTION</b>", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = col.TextSecondary },
                RectTransform = { AnchorMin = "0.42 0.920", AnchorMax = "0.97 0.970" }
            }, UI_CONTENT);

            c.Add(new CuiPanel
            {
                Image         = { Color = col.SidebarDiv },
                RectTransform = { AnchorMin = "0.03 0.913", AnchorMax = "0.97 0.916" }
            }, UI_CONTENT);

            c.Add(new CuiPanel
            {
                Image         = { Color = col.SidebarDiv },
                RectTransform = { AnchorMin = "0.407 0.04", AnchorMax = "0.410 0.912" }
            }, UI_CONTENT);

            const float startY = 0.905f;
            const float rowH   = 0.108f;

            for (int i = 0; i < _config.Commands.Count; i++)
            {
                float yMax = startY - i * rowH;
                float yMin = yMax - rowH + 0.004f;
                if (yMin < 0.03f) break;

                if (i % 2 == 0)
                {
                    c.Add(new CuiPanel
                    {
                        Image         = { Color = "1 1 1 0.018" },
                        RectTransform = { AnchorMin = $"0.03 {yMin:F3}", AnchorMax = $"0.97 {yMax:F3}" }
                    }, UI_CONTENT);
                }

                var entry = _config.Commands[i];

                c.Add(new CuiLabel
                {
                    Text          = { Text = $"<b>{entry.Cmd}</b>", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = col.Gold, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = $"0.040 {yMin:F3}", AnchorMax = $"0.400 {yMax:F3}" }
                }, UI_CONTENT);

                c.Add(new CuiLabel
                {
                    Text          = { Text = entry.Desc, FontSize = 11, Align = TextAnchor.MiddleLeft, Color = col.TextPrimary },
                    RectTransform = { AnchorMin = $"0.420 {yMin:F3}", AnchorMax = $"0.970 {yMax:F3}" }
                }, UI_CONTENT);
            }
        }

        #endregion

        #region Utilities

        private static string Hex(string rgba)
        {
            var p = rgba.Split(' ');
            int r = Mathf.RoundToInt(float.Parse(p[0], CultureInfo.InvariantCulture) * 255f);
            int g = Mathf.RoundToInt(float.Parse(p[1], CultureInfo.InvariantCulture) * 255f);
            int b = Mathf.RoundToInt(float.Parse(p[2], CultureInfo.InvariantCulture) * 255f);
            return $"{r:X2}{g:X2}{b:X2}";
        }

        #endregion
    }
}
