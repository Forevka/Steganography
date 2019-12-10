using System;
using System.Drawing;
using System.Text;

namespace Steganography
{
    class SteganographyHelper
    {
        public enum State
        {
            Hiding,
            Filling_With_Zeros
        };

        public static Bitmap embedText(string text, Bitmap bmp)
        {
            // спочатку ми будемо ховати текст в картинці
            State state = State.Hiding;

            int charIndex = 0;

            // змінна для збереження інтової інтерпретації символу
            int charValue = 0;

            // запамятовуємо текущій піксель
            long pixelElementIndex = 0;

            // зберігаємо число нулів які були додані на кінці обробки
            int zeros = 0;
            
            int R = 0, G = 0, B = 0;

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    if (!((i % 2 == 0) && (j % 2 == 0)))
                        continue;

                    // отримуємо піксел
                    Color pixel = bmp.GetPixel(j, i);

                    // тепер очистимо наймолодший біт (LSB) від кожного пікселя
                    R = pixel.R - pixel.R % 2;
                    G = pixel.G - pixel.G % 2;
                    B = pixel.B - pixel.B % 2;

                    // проходимо по елементам кожного пікселя
                    for (int n = 0; n < 3; n++)
                    {
                        // перевіряємо, чи було оброблено нових 8 біт
                        if (pixelElementIndex % 8 == 0)
                        {
                            // перевіряємо, чи закінчився весь процес
                            // можемо сказати шо процес закінчився коли додано 8 нулів
                            if (state == State.Filling_With_Zeros && zeros == 8)
                            {
                                // встановлюємо дані для пікселя навіть якшо була порушена частина його елементів
                                if ((pixelElementIndex - 1) % 3 < 2)
                                {
                                    bmp.SetPixel(j, i, Color.FromArgb(R, G, B));
                                }

                                // повертаємо картинку 
                                return bmp;
                            }

                            // перевіряємо чи весь текст був закриптований
                            if (charIndex >= text.Length)
                            {
                                // якшо весь то тепер записуємо все остане місце нулями
                                state = State.Filling_With_Zeros;
                            }
                            else
                            {
                                // переходимо до наступної букви
                                charValue = text[charIndex++];
                            }
                        }

                        // дивимося який елемент пікселя треба опрацювати
                        switch (pixelElementIndex % 3)
                        {
                            case 0:
                                {
                                    if (state == State.Hiding)
                                    {
                                        // крайній правий біт символу буде [charValue % 2]
                                        // поставимо це значення замість LSB елемента пікселя так як ми його очистили перед цією операцією
                                        R += charValue % 2;

                                        // видаляємо крайній правий біт 
                                        // щоб в наступний раз ми змогли дістатись до нього
                                        charValue /= 2;
                                    }
                                } break;
                            case 1:
                                {
                                    if (state == State.Hiding)
                                    {
                                        G += charValue % 2;

                                        charValue /= 2;
                                    }
                                } break;
                            case 2:
                                {
                                    if (state == State.Hiding)
                                    {
                                        B += charValue % 2;

                                        charValue /= 2;
                                    }

                                    bmp.SetPixel(j, i, Color.FromArgb(R, G, B));
                                } break;
                        }

                        pixelElementIndex++;

                        if (state == State.Filling_With_Zeros)
                        {
                            // додаємо до нулів +1 поки вони не будуть 8
                            zeros++;
                        }
                    }
                }
            }

            return bmp;
        }

        public static string extractText(Bitmap bmp)
        {
            int colorUnitIndex = 0;
            int charValue = 0;

            // ініціюєємо пусту змінну для тексту
            string extractedText = String.Empty;

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    if (!((i % 2 == 0) && (j % 2 == 0)))
                        continue;
                    Color pixel = bmp.GetPixel(j, i);
                    
                    for (int n = 0; n < 3; n++)
                    {
                        switch (colorUnitIndex % 3)
                        {
                            case 0:
                                {
                                    charValue = charValue * 2 + pixel.R % 2;
                                } break;
                            case 1:
                                {
                                    charValue = charValue * 2 + pixel.G % 2;
                                } break;
                            case 2:
                                {
                                    charValue = charValue * 2 + pixel.B % 2;
                                } break;
                        }

                        colorUnitIndex++;

                        // якшо було опрацьовано 8 бітів то додаємо букву до змінної extractedText
                        if (colorUnitIndex % 8 == 0)
                        {
                            // оскільки кожен раз процес відбувається справа то проходимся по бітах в реверсі
                            charValue = reverseBits(charValue);

                            // може бути нуль якшо це кінець
                            if (charValue == 0)
                            {
                                return UTF8ToWin1251(extractedText);
                            }

                            var c = char.ConvertFromUtf32(charValue + 1024);//(char)charValue;

                            extractedText += c.ToString();
                        }
                    }
                }
            }

            return UTF8ToWin1251(extractedText);
        }

        static string UTF8ToWin1251(string sourceStr)
        {
            Encoding utf8 = Encoding.UTF8;
            Encoding win1251 = Encoding.GetEncoding("Windows-1251");
            byte[] utf8Bytes = utf8.GetBytes(sourceStr);
            byte[] win1251Bytes = Encoding.Convert(utf8, win1251, utf8Bytes);
            return win1251.GetString(win1251Bytes);
        }

        public static int reverseBits(int n)
        {
            int result = 0;

            for (int i = 0; i < 8; i++)
            {
                result = result * 2 + n % 2;

                n /= 2;
            }

            return result;
        }
    }
}
