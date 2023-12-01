using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service.Clipboard
{
    public interface IClipboard
    {
        string GetClipboardText();
        void SetClipboardText(string text);
    }
}
