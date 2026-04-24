using ImGuiNET;
using Riptide;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Phoenix.Framework.Rendering.GUI
{
    public static class ErrorListWindow
    {
        public static bool Show { get; set; } = false;
        private static Dictionary<string, ErrorItem> _errors = new();
        private static UI _ui;
                
        public static void Add(
            string error,
            [CallerFilePath] string filePath ="",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "",
            float showTimeSeconds = 0)
        {
            if(!_errors.TryGetValue(error, out var item))
            {
                string fileName = Path.GetFileName(filePath);
                
                item = new ErrorItem
                {
                    MaxTime = showTimeSeconds,
                    CallerInfo = $"[{fileName}:{lineNumber}] ({memberName})"
                };

                _errors.Add(error, item);
            }
            item.Count++;
            item.CurrentTime = 0;
            Show = true;
        }
        internal static void SetUI(UI ui)
        {
            _ui = ui;
        }
        internal static void Update(float deltaTime)
        {
            var items = _errors.Values;
            
            foreach (var item in items)
                item.CurrentTime += deltaTime;
            
        }
        internal static void Render()
        {
            if (!Show)
                return;

            _ui.SetFontSize(15);
            ImGui.Begin("Error List", ImGuiWindowFlags.AlwaysAutoResize);
            
            foreach (var item in _errors)
            {
                var val = item.Value;
                if (val.MaxTime != 0 && val.CurrentTime > val.MaxTime)
                    continue;

                var count = val.Count > 1 ? $"({val.Count}) " : "";
                
                ImGui.Text($"{count}{item.Key}");
                if(ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(item.Value.CallerInfo);
                }
            }

            ImGui.End();
        }
    }
}
