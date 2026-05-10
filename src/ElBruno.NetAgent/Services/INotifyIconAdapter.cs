using System;
using System.Windows.Forms;

namespace ElBruno.NetAgent.Services
{
    // Internal adapter to isolate System.Windows.Forms.NotifyIcon for unit tests
    internal interface INotifyIconAdapter : IDisposable
    {
        bool Visible { get; set; }
        string Text { get; set; }
        System.Drawing.Icon Icon { get; set; }
        ContextMenuStrip ContextMenuStrip { get; set; }
        void ShowBalloonTip(int timeout, string title, string text, ToolTipIcon icon);
        bool IsDisposed { get; }
    }

    internal class NotifyIconAdapter : INotifyIconAdapter
    {
        private readonly NotifyIcon _inner;
        private bool _disposed;

        public NotifyIconAdapter(NotifyIcon inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool Visible
        {
            get => _inner.Visible;
            set => _inner.Visible = value;
        }

        public string Text
        {
            get => _inner.Text;
            set => _inner.Text = value;
        }

        public System.Drawing.Icon Icon
        {
            get => _inner.Icon;
            set => _inner.Icon = value;
        }

        public ContextMenuStrip ContextMenuStrip
        {
            get => _inner.ContextMenuStrip;
            set => _inner.ContextMenuStrip = value;
        }

        public void ShowBalloonTip(int timeout, string title, string text, ToolTipIcon icon)
        {
            try { _inner.ShowBalloonTip(timeout, title, text, icon); } catch { }
        }

        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            try { _inner.Dispose(); } catch { }
            _disposed = true;
        }
    }
}
