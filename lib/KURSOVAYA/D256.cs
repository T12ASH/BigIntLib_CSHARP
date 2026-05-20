using System;

namespace KURSOVAYA
{
    public class D256
    {
        private const int WORD_BITS = 64;
        private const int WORD_COUNT = 4;
        private const int BYTE_COUNT = 32;

        private ulong[] words = new ulong[WORD_COUNT];
        private bool negative = false;
        private bool isNaN = false;

        // ----------------------------------------------------------
        // Вспомогательные статические методы
        // ----------------------------------------------------------
        internal ulong[] GetWords() { return words; }
        private static void Mul64By10(ulong a, out ulong low, out ulong high)
        {
            ulong a_low = a & 0xFFFFFFFF;
            ulong a_high = a >> 32;
            ulong p_low = a_low * 10;
            ulong p_high = a_high * 10;
            ulong carry = p_low >> 32;
            ulong mid = (p_high & 0xFFFFFFFF) + carry;
            high = (p_high >> 32) + (mid >> 32);
            low = ((mid & 0xFFFFFFFF) << 32) | (p_low & 0xFFFFFFFF);
        }

        private static void Mul64x64(ulong a, ulong b, out ulong low, out ulong high)
        {
            ulong a_low = a & 0xFFFFFFFF;
            ulong a_high = a >> 32;
            ulong b_low = b & 0xFFFFFFFF;
            ulong b_high = b >> 32;

            ulong p1 = a_low * b_low;
            ulong p2 = a_low * b_high;
            ulong p3 = a_high * b_low;
            ulong p4 = a_high * b_high;

            ulong mid = p2 + p3;
            ulong carry = (mid < p2) ? 1UL : 0UL;

            low = p1 + (mid << 32);
            high = p4 + (mid >> 32) + carry;
            if (low < p1) high++;
        }

        private static void Div256By10(ulong[] w, out ulong remainder)
        {
            ulong carry = 0;
            for (int i = WORD_COUNT - 1; i >= 0; i--)
            {
                uint high = (uint)(w[i] >> 32);
                uint low = (uint)(w[i] & 0xFFFFFFFF);
                ulong temp1 = (carry << 32) | high;
                uint q_high = (uint)(temp1 / 10);
                carry = temp1 % 10;
                ulong temp2 = (carry << 32) | low;
                uint q_low = (uint)(temp2 / 10);
                carry = temp2 % 10;
                w[i] = ((ulong)q_high << 32) | q_low;
            }
            remainder = carry;
        }

        private static int CompareArrays(ulong[] a, ulong[] b)
        {
            for (int i = WORD_COUNT - 1; i >= 0; i--)
                if (a[i] != b[i])
                    return a[i] < b[i] ? -1 : 1;
            return 0;
        }
        public void SetBit(ulong value, uint index)
        {
            if (index < WORD_COUNT * WORD_BITS) { throw new ArgumentOutOfRangeException("index"); }
            if (value > 1) throw new ArgumentOutOfRangeException(nameof(value));

            uint w = index / 64;
            uint b = index % 64;
            ulong mask = 1UL << (int)b;

            words[w] = (words[w] & ~mask) | (value << (int)b);
        }
        public bool GetBit(uint index)
        {
            if (index >= WORD_COUNT * WORD_BITS) { throw new ArgumentOutOfRangeException("index"); }

            uint w = index / 64;
            uint b = index % 64;

            bool res = Convert.ToBoolean((words[w] >> (int)b) & 1UL);

            return res;
        }

        // ----------------------------------------------------------
        // Конструкторы
        // ----------------------------------------------------------
        public D256() { }

        public D256(long value)
        {
            if (value < 0)
            {
                negative = true;
                value = -value;
            }
            words[0] = (ulong)value;
        }

        public D256(string str)
        {
            if (string.IsNullOrEmpty(str))
                throw new ArgumentException("D256: пустая строка");

            int start = 0;
            if (str[0] == '-')
            {
                negative = true;
                start = 1;
                if (start >= str.Length)
                    throw new ArgumentException("D256: после минуса нет цифр");
            }

            if (str[start] == '0' && str.Length - start > 1)
                throw new ArgumentException("D256: число не может начинаться с нуля");

            for (int i = start; i < str.Length; i++)
            {
                if (!char.IsDigit(str[i]))
                    throw new ArgumentException($"D256: недопустимый символ '{str[i]}'");
                if (str[i] == '-')
                    throw new ArgumentException("D256: минус может быть только первым символом");
            }

            if (str.Substring(start) == "0")
            {
                negative = false;
                return;
            }

            for (int i = start; i < str.Length; i++)
            {
                ulong digit = (ulong)(str[i] - '0');
                ulong carry = digit;

                for (int word = 0; word < WORD_COUNT; word++)
                {
                    Mul64By10(words[word], out ulong low, out ulong high);
                    ulong sum = low + carry;
                    bool overflow = (sum < low);
                    words[word] = sum;
                    carry = high + (overflow ? 1UL : 0UL);
                }

                if (carry != 0)
                    throw new OverflowException("D256: число превышает 256 бит");
            }

            if (IsZero()) negative = false;
        }

        public D256(string str, int baseValue)
        {

            if (baseValue < 2 || baseValue > 36)
            {
                isNaN = true;
                throw new ArgumentException("D512: base must be between 2 and 36");
            }

            if (string.IsNullOrEmpty(str))
            {
                isNaN = true;
                throw new ArgumentException("D512: empty string");
            }

            int start = 0;
            if (str[0] == '-')
            {
                negative = true;
                start = 1;
                if (start >= str.Length)
                {
                    isNaN = true;
                    throw new ArgumentException("D512: no digits after sign");
                }
            }

            for (int i = start; i < str.Length; ++i)
            {
                char c = str[i];
                ulong digit;

                if (c >= '0' && c <= '9')
                    digit = (ulong)(c - '0');
                else if (c >= 'A' && c <= 'Z')
                    digit = 10 + (ulong)(c - 'A');
                else if (c >= 'a' && c <= 'z')
                    digit = 10 + (ulong)(c - 'a');
                else
                {
                    isNaN = true;
                    throw new ArgumentException("D512: invalid character");
                }

                if (digit >= (ulong)baseValue)
                {
                    isNaN = true;
                    throw new ArgumentException("D512: digit out of range for given base");
                }

                ulong carry = digit;
                for (int word = 0; word < WORD_COUNT; ++word)
                {
                    Mul64x64(words[word], (ulong)baseValue, out ulong low, out ulong high);

                    ulong sum = low + carry;
                    bool overflow = (sum < low);

                    words[word] = sum;
                    carry = high + (overflow ? 1UL : 0UL);
                }

                if (carry != 0)
                {
                    isNaN = true;
                    throw new OverflowException("D512: number too large for 512 bits");
                }
            }

            if (IsZero())
            {
                negative = false;
            }
        }


        public D256(D256 other)
        {
            Array.Copy(other.words, words, WORD_COUNT);
            negative = other.negative;
            isNaN = other.isNaN;
        }

        public D256(D256 other, bool move)
        {
            Array.Copy(other.words, words, WORD_COUNT);
            negative = other.negative;
            isNaN = other.isNaN;
            Array.Clear(other.words, 0, WORD_COUNT);
            other.negative = false;
            other.isNaN = false;
        }

        // ----------------------------------------------------------
        // Методы вместо свойств
        // ----------------------------------------------------------
        public bool IsZero()
        {
            foreach (ulong w in words) if (w != 0) return false;
            return true;
        }

        public bool IsOne()
        {
            if (negative) return false;
            for (int i = 1; i < WORD_COUNT; i++) if (words[i] != 0) return false;
            return words[0] == 1;
        }

        public bool IsNegative()
        {
            return negative;
        }

        public bool IsNaN()
        {
            return isNaN;
        }

        // ----------------------------------------------------------
        // Преобразования
        // ----------------------------------------------------------
        public override string ToString()
        {
            if (isNaN) return "NaN";
            if (IsZero()) return "0";

            D256 temp = new D256(this);
            temp.negative = false;
            string result = "";
            while (!temp.IsZero())
            {
                Div256By10(temp.words, out ulong rem);
                result = ((char)('0' + rem)).ToString() + result;
            }
            if (negative) result = "-" + result;
            return result;
        }

        public string ToHex()
        {
            string hex = "";
            for (int i = WORD_COUNT - 1; i >= 0; i--)
                hex += words[i].ToString("X16");
            if (negative) hex = "-" + hex;
            return hex;
        }

        public void PrintHex()
        {
            Console.Write(ToHex());
        }

        public override int GetHashCode()
        {
            int h = 0;
            foreach (ulong w in words) h ^= w.GetHashCode();
            h ^= negative.GetHashCode();
            return h;
        }

        public override bool Equals(object obj)
        {
            if (obj is D256 other)
                return this == other;
            return false;
        }

        public static D256 Max()
        {
            D256 result = new D256();
            for (int i = 0; i < WORD_COUNT; i++) result.words[i] = ulong.MaxValue;
            result.negative = false;
            return result;
        }

        public static D256 Min()
        {
            D256 result = Max();
            result.negative = true;
            return result;
        }

        public D256 Pow(long exp)
        {
            if (exp == 0) return new D256(1);
            if (exp < 0) return new D256(0);
            D256 result = new D256(this);
            for (long i = 1; i < exp; i++) result = result * this;
            return result;
        }

        // ----------------------------------------------------------
        // Операторы сравнения
        // ----------------------------------------------------------
        public static bool operator ==(D256 a, D256 b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            if (a.isNaN || b.isNaN) return false;
            if (a.negative != b.negative) return false;
            for (int i = 0; i < WORD_COUNT; i++)
                if (a.words[i] != b.words[i]) return false;
            return true;
        }

        public static bool operator !=(D256 a, D256 b) => !(a == b);

        public static bool operator <(D256 a, D256 b)
        {
            if (a.isNaN || b.isNaN) return false;
            if (a.negative != b.negative) return a.negative;
            for (int i = WORD_COUNT - 1; i >= 0; i--)
                if (a.words[i] != b.words[i])
                    return a.negative ? a.words[i] > b.words[i] : a.words[i] < b.words[i];
            return false;
        }

        public static bool operator >(D256 a, D256 b) => b < a;
        public static bool operator <=(D256 a, D256 b) => !(b < a);
        public static bool operator >=(D256 a, D256 b) => !(a < b);

        // ----------------------------------------------------------
        // Арифметические операторы
        // ----------------------------------------------------------
        public static D256 operator +(D256 a, D256 b)
        {
            if (a.isNaN || b.isNaN) return new D256 { isNaN = true };
            if (a.negative != b.negative)
            {
                D256 temp = new D256(b);
                temp.negative = !temp.negative;
                return a - temp;
            }
            D256 res = new D256();
            ulong carry = 0;
            for (int i = 0; i < WORD_COUNT; i++)
            {
                ulong sum = a.words[i] + b.words[i] + carry;
                carry = (sum < a.words[i] || (carry == 1 && sum == a.words[i])) ? 1UL : 0UL;
                res.words[i] = sum;
            }
            res.negative = a.negative;
            if (carry != 0) res.isNaN = true;
            return res;
        }

        public static D256 operator -(D256 a, D256 b)
        {
            if (a.isNaN || b.isNaN) return new D256 { isNaN = true };
            if (a.negative != b.negative)
            {
                D256 temp = new D256(b);
                temp.negative = !temp.negative;
                return a + temp;
            }

            bool absLess = false;
            for (int i = WORD_COUNT - 1; i >= 0; i--)
                if (a.words[i] != b.words[i])
                {
                    absLess = a.words[i] < b.words[i];
                    break;
                }
            bool resultNegative = a.negative ? !absLess : absLess;
            D256 larger, smaller;
            if (absLess) { larger = b; smaller = a; }
            else { larger = a; smaller = b; }

            D256 res = new D256();
            ulong borrow = 0;
            for (int i = 0; i < WORD_COUNT; i++)
            {
                ulong diff = larger.words[i] - smaller.words[i] - borrow;
                borrow = (diff > larger.words[i] || (borrow == 1 && diff == larger.words[i])) ? 1UL : 0UL;
                res.words[i] = diff;
            }
            res.negative = resultNegative;
            return res;
        }

        public static D256 operator *(D256 a, D256 b)
        {
            if (a.isNaN || b.isNaN) return new D256 { isNaN = true };
            D256 res = new D256();
            res.negative = a.negative != b.negative;
            ulong[] temp = new ulong[WORD_COUNT * 2];
            for (int i = 0; i < WORD_COUNT; i++)
            {
                if (a.words[i] == 0) continue;
                ulong carry = 0;
                for (int j = 0; j < WORD_COUNT; j++)
                {
                    Mul64x64(a.words[i], b.words[j], out ulong low, out ulong high);
                    ulong sum = temp[i + j] + low + carry;
                    bool ov1 = sum < low;
                    bool ov2 = sum < carry;
                    temp[i + j] = sum;
                    carry = high + (ov1 ? 1UL : 0UL) + (ov2 ? 1UL : 0UL);
                }
                if (carry != 0) temp[i + WORD_COUNT] += carry;
            }
            for (int i = 0; i < WORD_COUNT; i++) res.words[i] = temp[i];
            for (int i = WORD_COUNT; i < WORD_COUNT * 2; i++)
                if (temp[i] != 0) { res.isNaN = true; break; }
            if (res.IsZero()) res.negative = false;
            return res;
        }

        public static D256 operator /(D256 a, D256 b)
        {
            if (a.isNaN || b.isNaN || b.IsZero()) return new D256 { isNaN = true };
            bool resultNegative = a.negative != b.negative;
            D256 dividend = new D256(a);
            D256 divisor = new D256(b);
            dividend.negative = false;
            divisor.negative = false;
            if (dividend < divisor) return new D256 { negative = resultNegative };

            ulong[] quotient = new ulong[WORD_COUNT];
            ulong[] remainder = new ulong[WORD_COUNT];
            for (int bit = 255; bit >= 0; bit--)
            {
                ulong carry = 0;
                for (int i = 0; i < WORD_COUNT; i++)
                {
                    ulong newCarry = remainder[i] >> 63;
                    remainder[i] = (remainder[i] << 1) | carry;
                    carry = newCarry;
                }
                int wordIdx = bit / 64;
                int bitIdx = bit % 64;
                if ((dividend.words[wordIdx] & (1UL << bitIdx)) != 0)
                    remainder[0] |= 1;

                if (CompareArrays(remainder, divisor.words) >= 0)
                {
                    ulong borrow = 0;
                    for (int i = 0; i < WORD_COUNT; i++)
                    {
                        ulong diff = remainder[i] - divisor.words[i] - borrow;
                        borrow = (diff > remainder[i] || (borrow == 1 && diff == remainder[i])) ? 1UL : 0UL;
                        remainder[i] = diff;
                    }
                    quotient[bit / 64] |= (1UL << (bit % 64));
                }
            }
            D256 res = new D256();
            res.negative = resultNegative;
            for (int i = 0; i < WORD_COUNT; i++) res.words[i] = quotient[i];
            if (res.IsZero()) res.negative = false;
            return res;
        }

        public static D256 operator %(D256 a, D256 b)
        {
            if (a.isNaN || b.isNaN || b.IsZero()) return new D256 { isNaN = true };
            D256 q = a / b;
            D256 p = q * b;
            D256 rem = a - p;
            if (!rem.IsZero() && a.negative != rem.negative)
                rem = rem + b;
            return rem;
        }

        // ----------------------------------------------------------
        // Инкремент/декремент
        // ----------------------------------------------------------
        public static D256 operator ++(D256 a)
        {
            D256 one = new D256(1);
            return a + one;
        }

        public static D256 operator --(D256 a)
        {
            D256 one = new D256(1);
            return a - one;
        }

        // ----------------------------------------------------------
        // Унарные операторы
        // ----------------------------------------------------------
        public static D256 operator -(D256 a)
        {
            D256 res = new D256(a);
            if (!res.IsZero()) res.negative = !res.negative;
            return res;
        }

        public static D256 operator +(D256 a) => a;

        // ----------------------------------------------------------
        // Битовые операторы
        // ----------------------------------------------------------
        public static D256 operator &(D256 a, D256 b)
        {
            D256 res = new D256();
            for (int i = 0; i < WORD_COUNT; i++) res.words[i] = a.words[i] & b.words[i];
            res.negative = a.negative & b.negative;
            res.isNaN = a.isNaN | b.isNaN;
            return res;
        }

        public static D256 operator |(D256 a, D256 b)
        {
            D256 res = new D256();
            for (int i = 0; i < WORD_COUNT; i++) res.words[i] = a.words[i] | b.words[i];
            res.negative = a.negative | b.negative;
            res.isNaN = a.isNaN | b.isNaN;
            return res;
        }

        public static D256 operator ^(D256 a, D256 b)
        {
            D256 res = new D256();
            for (int i = 0; i < WORD_COUNT; i++) res.words[i] = a.words[i] ^ b.words[i];
            res.negative = a.negative ^ b.negative;
            res.isNaN = a.isNaN | b.isNaN;
            return res;
        }

        public static D256 operator ~(D256 a)
        {
            D256 res = new D256();
            for (int i = 0; i < WORD_COUNT; i++) res.words[i] = ~a.words[i];
            res.negative = !a.negative;
            res.isNaN = a.isNaN;
            return res;
        }

        public static D256 operator <<(D256 a, int shift)
        {
            if (shift <= 0) return a;
            D256 res = new D256();
            int wordShift = shift / 64;
            int bitShift = shift % 64;
            if (wordShift >= WORD_COUNT) return res;
            for (int i = 0; i < WORD_COUNT - wordShift; i++)
                res.words[i + wordShift] = a.words[i] << bitShift;
            if (bitShift != 0)
                for (int i = 0; i < WORD_COUNT - wordShift - 1; i++)
                    res.words[i + wordShift + 1] |= a.words[i] >> (64 - bitShift);
            res.negative = a.negative;
            res.isNaN = a.isNaN;
            return res;
        }

        public static D256 operator >>(D256 a, int shift)
        {
            if (shift <= 0) return a;
            D256 res = new D256();
            int wordShift = shift / 64;
            int bitShift = shift % 64;
            if (wordShift >= WORD_COUNT) return res;
            for (int i = wordShift; i < WORD_COUNT; i++)
                res.words[i - wordShift] = a.words[i] >> bitShift;
            if (bitShift != 0)
                for (int i = wordShift + 1; i < WORD_COUNT; i++)
                    res.words[i - wordShift - 1] |= a.words[i] << (64 - bitShift);
            res.negative = a.negative;
            res.isNaN = a.isNaN;
            return res;
        }

        // ----------------------------------------------------------
        // Преобразование в long
        // ----------------------------------------------------------
        public static explicit operator long(D256 a)
        {
            if (a.negative) return -(long)a.words[0];
            return (long)a.words[0];
        }

        public string ToBase(int baseValue)
        {
            if (baseValue < 2 || baseValue > 36)
            {
                throw new ArgumentException("D512: base must be between 2 and 36");
            }

            if (isNaN) return "NaN";
            if (IsZero()) return "0";

            D256 temp = new D256(this);
            temp.negative = false;
            D256 divisor = new D256(baseValue);

            
            char[] chars = new char[600];
            int charCount = 0;

            while (!temp.IsZero())
            {
                D256 remainder = temp % divisor;
                temp = temp / divisor;

                ulong digit = remainder.words[0];
                char ch;

                if (digit < 10)
                {
                    ch = (char)('0' + (uint)digit);
                }
                else
                {
                    ch = (char)('A' + (uint)(digit - 10));
                }

                chars[charCount++] = ch;
            }

            if (negative)
            {
                chars[charCount++] = '-';
            }

            Array.Reverse(chars, 0, charCount);

            return new string(chars, 0, charCount);
        }

    }
}