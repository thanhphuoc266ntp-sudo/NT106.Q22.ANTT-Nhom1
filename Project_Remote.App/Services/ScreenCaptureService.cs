using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Forms = System.Windows.Forms;

namespace RemoteMate.Services
{
    public class ScreenCaptureService : IDisposable
    {
        private Bitmap? _bitmap;
        private Graphics? _graphics;
        private readonly ImageCodecInfo? _jpegCodec;

        public ScreenCaptureService()
        {
            _jpegCodec = GetEncoderInfo("image/jpeg");
        }

        public byte[] CaptureScreen(int quality = 50)
        {
            quality = Math.Clamp(quality, 10, 100);

            Rectangle bounds = Forms.Screen.PrimaryScreen.Bounds;

            if (_bitmap == null || _bitmap.Width != bounds.Width || _bitmap.Height != bounds.Height)
            {
                _graphics?.Dispose();
                _bitmap?.Dispose();

                _bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
                _graphics = Graphics.FromImage(_bitmap);
            }

            if (_graphics == null || _jpegCodec == null)
                return Array.Empty<byte>();

            _graphics.CopyFromScreen(0, 0, 0, 0, bounds.Size);

            using (EncoderParameters encoderParams = new EncoderParameters(1))
            {
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                using (MemoryStream ms = new MemoryStream())
                {
                    _bitmap.Save(ms, _jpegCodec, encoderParams);
                    return ms.ToArray();
                }
            }
        }

        private ImageCodecInfo? GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.MimeType == mimeType)
                    return codec;
            }

            return null;
        }

        public void Dispose()
        {
            _graphics?.Dispose();
            _bitmap?.Dispose();

            _graphics = null;
            _bitmap = null;
        }
    }
}