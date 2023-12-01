using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service.Clipboard
{
    public class SimpleClipboard : IClipboard
    {
        public string GetClipboardText()
        {
            return System.Windows.Clipboard.GetText();
        }

        public void SetClipboardText(string text)
        {
            System.Windows.Clipboard.SetText(text);
        }
    }
}
