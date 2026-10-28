using System;

namespace G3D
{
    internal static class BayerMHC
    {
        private static readonly float[,] G_GRR = new float[,]
        {
            { 0.0f, 0.0f, -1.0f, 0.0f, 0.0f },
            { 0.0f, 0.0f, 2.0f, 0.0f, 0.0f },
            { -1.0f, 2.0f, 4.0f, 2.0f, -1.0f },
            { 0.0f, 0.0f, 2.0f, 0.0f, 0.0f },
            { 0.0f, 0.0f, -1.0f, 0.0f, 0.0f }
        };

        private static readonly float[,] G_BGB = new float[,]
        {
            { 0.0f, 0.0f, -1.0f, 0.0f, 0.0f },
            { 0.0f, 0.0f, 2.0f, 0.0f, 0.0f },
            { -1.0f, 2.0f, 4.0f, 2.0f, -1.0f },
            { 0.0f, 0.0f, 2.0f, 0.0f, 0.0f },
            { 0.0f, 0.0f, -1.0f, 0.0f, 0.0f }
        };

        private static readonly float[,] R_GRG = new float[,]
        {
            { 0.0f, 0.0f, 0.5f, 0.0f, 0.0f },
            { 0.0f, -1.0f, 0.0f, -1.0f, 0.0f },
            { -1.0f, 4.0f, 5.0f, 4.0f, -1.0f },
            { 0.0f, -1.0f, 0.0f, -1.0f, 0.0f },
            { 0.0f, 0.0f, 0.5f, 0.0f, 0.0f }
        };

        private static readonly float[,] R_BGG = new float[,]
        {
            { 0.0f, 0.0f, -1.0f, 0.0f, 0.0f },
            { 0.0f, -1.0f, 4.0f, -1.0f, 0.0f },
            { 0.5f, 0.0f, 5.0f, 0.0f, 0.5f },
            { 0.0f, -1.0f, 4.0f, -1.0f, 0.0f },
            { 0.0f, 0.0f, -1.0f, 0.0f, 0.0f }
        };

        private static readonly float[,] R_BGB = new float[,]
        {
            { 0.0f, 0.0f, -1.5f, 0.0f, 0.0f },
            { 0.0f, 2.0f, 0.0f, 2.0f, 0.0f },
            { -1.5f, 0.0f, 6.0f, 0.0f, -1.5f },
            { 0.0f, 2.0f, 0.0f, 2.0f, 0.0f },
            { 0.0f, 0.0f, -1.5f, 0.0f, 0.0f }
        };

        internal static void bayerG8B8R8G8(int w, int h, byte[] input, byte[] output)
        {
            validateMHC(w, h, input, output);
            for (int y = 0; y < h; y += 2)
            {
                for (int x = 0; x < w; x += 2)
                {
                    int outputIndex = (x + y * w) * 3;
                    output[outputIndex] = applyFilter(input, x, y, w, h, R_BGG);
                    output[outputIndex + 1] = input[x + y * w];
                    output[outputIndex + 2] = applyFilter(input, x, y, w, h, B_BGG());

                    outputIndex += 3;
                    output[outputIndex] = applyFilter(input, x + 1, y, w, h, R_BGB);
                    output[outputIndex + 1] = applyFilter(input, x + 1, y, w, h, G_BGB);
                    output[outputIndex + 2] = input[x + 1 + y * w];

                    outputIndex = (x + (y + 1) * w) * 3;
                    output[outputIndex] = input[x + (y + 1) * w];
                    output[outputIndex + 1] = applyFilter(input, x, y + 1, w, h, G_GRR);
                    output[outputIndex + 2] = applyFilter(input, x, y + 1, w, h, B_GRR());

                    outputIndex += 3;
                    output[outputIndex] = applyFilter(input, x + 1, y + 1, w, h, R_GRG);
                    output[outputIndex + 1] = input[x + 1 + (y + 1) * w];
                    output[outputIndex + 2] = applyFilter(input, x + 1, y + 1, w, h, B_GRG());
                }
            }
        }

        internal static void bayerR8G8G8B8(int w, int h, byte[] input, byte[] output)
        {
            validateMHC(w, h, input, output);
            for (int y = 0; y < h; y += 2)
            {
                for (int x = 0; x < w; x += 2)
                {
                    int outputIndex = (x + y * w) * 3;
                    output[outputIndex] = input[x + y * w];
                    output[outputIndex + 1] = applyFilter(input, x, y, w, h, G_GRR);
                    output[outputIndex + 2] = applyFilter(input, x, y, w, h, B_GRR());

                    outputIndex += 3;
                    output[outputIndex] = applyFilter(input, x + 1, y, w, h, R_GRG);
                    output[outputIndex + 1] = input[x + 1 + y * w];
                    output[outputIndex + 2] = applyFilter(input, x + 1, y, w, h, B_GRG());

                    outputIndex = (x + (y + 1) * w) * 3;
                    output[outputIndex] = applyFilter(input, x, y + 1, w, h, R_BGG);
                    output[outputIndex + 1] = input[x + (y + 1) * w];
                    output[outputIndex + 2] = applyFilter(input, x, y + 1, w, h, B_BGG());

                    outputIndex += 3;
                    output[outputIndex] = applyFilter(input, x + 1, y + 1, w, h, R_BGB);
                    output[outputIndex + 1] = applyFilter(input, x + 1, y + 1, w, h, G_BGB);
                    output[outputIndex + 2] = input[x + 1 + (y + 1) * w];
                }
            }
        }

        internal static void bayerG8R8B8G8(int w, int h, byte[] input, byte[] output)
        {
            bayerG8B8R8G8(w, h, input, output);
            swapRedAndBlue(w * h, output);
        }

        internal static void bayerB8G8G8R8(int w, int h, byte[] input, byte[] output)
        {
            bayerR8G8G8B8(w, h, input, output);
            swapRedAndBlue(w * h, output);
        }

        internal static void bayerToQuarter(int width, int height, byte[] input, byte[] output)
        {
            if (width < 0 || height < 0 || (width & 1) != 0 || (height & 1) != 0)
            {
                throw new ArgumentException("Bayer conversion requires even dimensions.");
            }
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            if (input.Length < width * height * 2 || output.Length < (width / 2) * (height / 2) * 3)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            int destination = 0;
            for (int y = 0; y < height / 2; ++y)
            {
                for (int x = 0; x < width / 2; ++x)
                {
                    int source = x * 2 + y * 2 * width;
                    output[destination] = input[source + width];
                    output[destination + 1] = (byte)((input[source] + input[source + width + 1]) / 2);
                    output[destination + 2] = input[source + 1];
                    destination += 3;
                }
            }
        }

        internal static void quarterToBayer(int inWidth, int inHeight, byte[] input, byte[] output)
        {
            if (inWidth < 0 || inHeight < 0)
            {
                throw new ArgumentException("Bayer conversion requires non-negative dimensions.");
            }
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            int outWidth = checked(inWidth * 2);
            int outHeight = checked(inHeight * 2);
            if (input.Length < inWidth * inHeight * 3 || output.Length < outWidth * outHeight)
            {
                throw new ArgumentException("The image buffer is too small.");
            }

            for (int y = 0; y < outHeight; ++y)
            {
                for (int x = 0; x < outWidth; ++x)
                {
                    int source = (x / 2 + (y / 2) * inWidth) * 3;
                    byte value;
                    if ((y & 1) == 0)
                    {
                        value = (x & 1) == 0 ? input[source + 1] : input[source + 2];
                    }
                    else
                    {
                        value = (x & 1) == 0 ? input[source] : input[source + 1];
                    }
                    output[x + y * outWidth] = value;
                }
            }
        }

        private static float[,] B_BGG()
        {
            return R_GRG;
        }

        private static float[,] B_GRG()
        {
            return R_BGG;
        }

        private static float[,] B_GRR()
        {
            return R_BGB;
        }

        private static byte applyFilter(byte[] input, int x, int y, int w, int h, float[,] filter)
        {
            float sum = 0.0f;
            float denominator = 0.0f;
            for (int dy = 0; dy < 5; ++dy)
            {
                int offset = ((y + dy + h - 2) % h) * w;
                for (int dx = 0; dx < 5; ++dx)
                {
                    float coefficient = filter[dy, dx];
                    sum += coefficient * input[(x + dx + w - 2) % w + offset];
                    denominator += coefficient;
                }
            }
            return (byte)g3dmath.iClamp(g3dmath.iRound(sum / denominator), 0, 255);
        }

        private static void swapRedAndBlue(int count, byte[] output)
        {
            for (int i = count - 1; i >= 0; --i)
            {
                byte temporary = output[i * 3];
                output[i * 3] = output[i * 3 + 2];
                output[i * 3 + 2] = temporary;
            }
        }

        private static void validateMHC(int w, int h, byte[] input, byte[] output)
        {
            if (w <= 0 || h <= 0 || (w & 1) != 0 || (h & 1) != 0)
            {
                throw new ArgumentException("Bayer conversion requires positive even dimensions.");
            }
            if (input == null || output == null)
            {
                throw new ArgumentNullException(input == null ? "input" : "output");
            }
            if (input.Length < w * h || output.Length < w * h * 3)
            {
                throw new ArgumentException("The image buffer is too small.");
            }
        }
    }
}
