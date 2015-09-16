using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace BzoneLauncher
{
    public class PalletCache
    {
        private static PalletCache instance;

        Dictionary<string, Color[]> ColorCache;

        private PalletCache()
        {
            ColorCache = new Dictionary<string, Color[]>();
        }

        public static PalletCache GetInstance()
        {
            if (instance == null) instance = new PalletCache();
            return instance;
        }

        public Image ColorizeImage(string Name, Image Image)
        {
            if (!ColorCache.ContainsKey(Name))
            {
                try
                {
                    ColorCache[Name] = Image2Pallet(Image.FromFile(@"Core\Pallet\" + Name + ".png"));
                }
                catch { return Image; }
            }
            return ColorizeImage(ColorCache[Name], Image);
        }

        private Bitmap ColorizeImage(Color[] Pallet, Image Image)
        {
            if (Pallet == null) return new Bitmap(Image);
            Bitmap sourceBitmap = new Bitmap(Image);

            for (int i = 0; i < Image.Width; i++)
            {
                for (int j = 0; j < Image.Height; j++)
                {
                    Color tmpColor = sourceBitmap.GetPixel(i, j);
                    Color newColor = Pallet[tmpColor.B];
                    sourceBitmap.SetPixel(i, j, Color.FromArgb(tmpColor.A, newColor));
                }
            }

            return sourceBitmap;
        }

        private Color[] Image2Pallet(Image Pallet = null)
        {
            if (Pallet != null)
            {
                Bitmap BalletBMP = new Bitmap(Pallet);
                Color[] OverlayPallet = new Color[256];
                for (int x = 0; x < Math.Min(Pallet.Width, 256); x++)
                {
                    OverlayPallet[x] = BalletBMP.GetPixel(x, 0);
                }
                return OverlayPallet;
            }
            else
            {
                Color[] OverlayPallet = new Color[256];
                for (int x = 0; x < 256; x++)
                {
                    OverlayPallet[x] = Color.FromArgb(x, x, x);
                }
                return OverlayPallet;
            }
        }
    }
}
