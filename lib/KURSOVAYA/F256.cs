using System;

namespace KURSOVAYA
{
    public class F256
    {
        private D256 mantissa;
        private long exponent;
        private bool negative;
        private bool isNaN;
        private bool isInf;

        private const int MAX_DECIMAL_DIGITS = 77;

        // ----------------------------------------------------------
        // Конструкторы
        // ----------------------------------------------------------
        public F256()
        {
            mantissa = new D256();
            exponent = 0;
            negative = false;
            isNaN = false;
            isInf = false;
        }

        public F256(F256 other)
        {
            mantissa = new D256(other.mantissa);
            exponent = other.exponent;
            negative = other.negative;
            isNaN = other.isNaN;
            isInf = other.isInf;
        }

        public F256(long value)
        {
            if (value < 0)
            {
                negative = true;
                value = -value;
            }
            mantissa = new D256(value);
            exponent = 0;
            isNaN = false;
            isInf = false;
            if (mantissa.IsZero()) negative = false;
        }

        public F256(double value)
        {
            exponent = 0;
            negative = false;
            isNaN = false;
            isInf = false;
            if (double.IsNaN(value))
            {
                isNaN = true;
                return;
            }
            if (double.IsInfinity(value))
            {
                isInf = true;
                negative = value < 0;
                return;
            }
            string s = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            F256 temp = new F256(s);
            mantissa = temp.mantissa;
            exponent = temp.exponent;
            negative = temp.negative;
            isNaN = temp.isNaN;
            isInf = temp.isInf;
        }

        public F256(D256 integer)
        {
            mantissa = new D256(integer);
            exponent = 0;
            negative = integer.IsNegative();
            isNaN = integer.IsNaN();
            isInf = false;
        }

        public F256(string str)
        {
            exponent = 0;
            negative = false;
            isNaN = false;
            isInf = false;

            if (string.IsNullOrEmpty(str) || str == "NaN")
            {
                isNaN = true;
                return;
            }
            if (str == "inf" || str == "+inf")
            {
                isInf = true;
                return;
            }
            if (str == "-inf")
            {
                isInf = true;
                negative = true;
                return;
            }

            int pos = 0;
            if (str[0] == '-')
            {
                negative = true;
                pos++;
            }
            else if (str[0] == '+')
                pos++;

            int dot = str.IndexOf('.', pos);
            int e = str.IndexOf('e', pos);
            if (e == -1) e = str.IndexOf('E', pos);

            string intPart = "", fracPart = "", expPart = "";
            int expSign = 1;

            if (dot != -1)
            {
                intPart = str.Substring(pos, dot - pos);
                if (e != -1)
                {
                    fracPart = str.Substring(dot + 1, e - dot - 1);
                    int estart = e + 1;
                    if (estart < str.Length && str[estart] == '-')
                    {
                        expSign = -1;
                        estart++;
                    }
                    else if (estart < str.Length && str[estart] == '+')
                        estart++;
                    expPart = str.Substring(estart);
                }
                else
                {
                    fracPart = str.Substring(dot + 1);
                }
            }
            else
            {
                if (e != -1)
                {
                    intPart = str.Substring(pos, e - pos);
                    int estart = e + 1;
                    if (estart < str.Length && str[estart] == '-')
                    {
                        expSign = -1;
                        estart++;
                    }
                    else if (estart < str.Length && str[estart] == '+')
                        estart++;
                    expPart = str.Substring(estart);
                }
                else
                {
                    intPart = str.Substring(pos);
                }
                fracPart = "";
            }

            if (string.IsNullOrEmpty(intPart)) intPart = "0";

            string full = intPart + fracPart;

            int firstNonZero = full.IndexOfAny(new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' });
            if (firstNonZero != -1)
            {
                full = full.Substring(firstNonZero);
                if (intPart != "0")
                    exponent += firstNonZero;
            }
            else
            {
                full = "0";
            }

            if (full.Length > MAX_DECIMAL_DIGITS)
            {
                exponent += full.Length - MAX_DECIMAL_DIGITS;
                full = full.Substring(0, MAX_DECIMAL_DIGITS);
            }

            if (full == "0")
            {
                mantissa = new D256(0);
                exponent = 0;
                negative = false;
                return;
            }

            mantissa = new D256(full);
            exponent -= fracPart.Length;

            if (!string.IsNullOrEmpty(expPart))
            {
                long expVal = long.Parse(expPart);
                exponent += (expSign == 1) ? expVal : -expVal;
            }

            Normalize();
        }


        // ----------------------------------------------------------
        // Нормализация
        // ----------------------------------------------------------
        private void Normalize()
        {
            if (mantissa.IsZero())
            {
                exponent = 0;
                negative = false;
                return;
            }
            D256 ten = new D256(10);
            D256 zero = new D256(0);
            while ((mantissa % ten) == zero && !mantissa.IsZero())
            {
                mantissa = mantissa / ten;
                exponent++;
            }
        }

        // ----------------------------------------------------------
        // Преобразования
        // ----------------------------------------------------------
        public override string ToString()
        {
            if (isNaN) return "NaN";
            if (isInf) return negative ? "-inf" : "inf";
            if (mantissa.IsZero()) return "0";

            string s = mantissa.ToString();
            long exp = exponent;

            while (s.Length > 1 && s[s.Length - 1] == '0')
            {
                s = s.Remove(s.Length - 1);
                exp++;
            }

            if (exp >= 0)
            {
                s = s + new string('0', (int)exp);
                return negative ? "-" + s : s;
            }

            long pointPos = s.Length + exp;
            if (pointPos <= 0)
            {
                string res = "0." + new string('0', (int)-pointPos) + s;
                return negative ? "-" + res : res;
            }
            else
            {
                string res = s.Substring(0, (int)pointPos) + "." + s.Substring((int)pointPos);
                return negative ? "-" + res : res;
            }
        }

        // ----------------------------------------------------------
        // Методы
        // ----------------------------------------------------------
        public bool IsZero()
        {
            return mantissa.IsZero() && !isNaN && !isInf;
        }

        public bool IsNaN()
        {
            return isNaN;
        }

        public bool IsInfinity()
        {
            return isInf;
        }

        public static F256 Zero()
        {
            return new F256();
        }

        public static F256 One()
        {
            return new F256("1");
        }

        public static F256 Max()
        {
            return new F256(D256.Max());
        }

        public static F256 Min()
        {
            return new F256(D256.Min());
        }

        // ----------------------------------------------------------
        // Унарные операторы
        // ----------------------------------------------------------
        public static F256 operator -(F256 a)
        {
            F256 res = new F256(a);
            if (!res.IsZero()) res.negative = !res.negative;
            return res;
        }

        public static F256 operator +(F256 a)
        {
            return a;
        }

        // ----------------------------------------------------------
        // Операторы сравнения
        // ----------------------------------------------------------
        public static bool operator ==(F256 a, F256 b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            if (a.isNaN || b.isNaN) return false;
            if (a.isInf || b.isInf) return a.isInf == b.isInf && a.negative == b.negative;
            if (a.IsZero() && b.IsZero()) return true;
            if (a.negative != b.negative) return false;

            F256 aa = new F256(a);
            F256 bb = new F256(b);
            aa.Normalize();
            bb.Normalize();

            D256 ten = new D256(10);
            if (aa.exponent > bb.exponent)
            {
                long diff = aa.exponent - bb.exponent;
                for (long i = 0; i < diff; i++)
                    aa.mantissa = aa.mantissa * ten;
                aa.exponent = bb.exponent;
            }
            else if (bb.exponent > aa.exponent)
            {
                long diff = bb.exponent - aa.exponent;
                for (long i = 0; i < diff; i++)
                    bb.mantissa = bb.mantissa * ten;
                bb.exponent = aa.exponent;
            }
            return aa.mantissa == bb.mantissa;
        }

        public static bool operator !=(F256 a, F256 b) => !(a == b);

        public static bool operator <(F256 a, F256 b)
        {
            if (a.isNaN || b.isNaN) return false;
            if (a.isInf || b.isInf)
            {
                if (a.isInf && b.isInf) return a.negative ? true : false;
                if (a.isInf) return a.negative;
                if (b.isInf) return !b.negative;
            }
            if (a.IsZero() && b.IsZero()) return false;
            if (a.negative != b.negative) return a.negative;

            F256 aa = new F256(a);
            F256 bb = new F256(b);
            D256 ten = new D256(10);
            if (aa.exponent > bb.exponent)
            {
                long diff = aa.exponent - bb.exponent;
                for (long i = 0; i < diff; i++)
                    aa.mantissa = aa.mantissa * ten;
                aa.exponent = bb.exponent;
            }
            else if (bb.exponent > aa.exponent)
            {
                long diff = bb.exponent - aa.exponent;
                for (long i = 0; i < diff; i++)
                    bb.mantissa = bb.mantissa * ten;
                bb.exponent = aa.exponent;
            }
            if (!a.negative)
                return aa.mantissa < bb.mantissa;
            else
                return aa.mantissa > bb.mantissa;
        }

        public static bool operator >(F256 a, F256 b) => b < a;
        public static bool operator <=(F256 a, F256 b) => !(b < a);
        public static bool operator >=(F256 a, F256 b) => !(a < b);

        // ----------------------------------------------------------
        // Арифметические операторы
        // ----------------------------------------------------------
        public static F256 operator +(F256 a, F256 b)
        {
            if (a.isNaN || b.isNaN) return new F256("NaN");
            if (a.isInf || b.isInf)
            {
                if (a.isInf && b.isInf && a.negative != b.negative) return new F256("NaN");
                return a.isInf ? a : b;
            }
            if (a.IsZero()) return b;
            if (b.IsZero()) return a;

            F256 aa = new F256(a);
            F256 bb = new F256(b);
            D256 ten = new D256(10);
            if (aa.exponent > bb.exponent)
            {
                long diff = aa.exponent - bb.exponent;
                for (long i = 0; i < diff; i++)
                    aa.mantissa = aa.mantissa * ten;
                aa.exponent = bb.exponent;
            }
            else if (bb.exponent > aa.exponent)
            {
                long diff = bb.exponent - aa.exponent;
                for (long i = 0; i < diff; i++)
                    bb.mantissa = bb.mantissa * ten;
                bb.exponent = aa.exponent;
            }

            F256 res = new F256();
            res.exponent = aa.exponent;
            if (a.negative == b.negative)
            {
                res.mantissa = aa.mantissa + bb.mantissa;
                res.negative = a.negative;
            }
            else
            {
                if (aa.mantissa == bb.mantissa) return new F256("0");
                if (aa.mantissa > bb.mantissa)
                {
                    res.mantissa = aa.mantissa - bb.mantissa;
                    res.negative = a.negative;
                }
                else
                {
                    res.mantissa = bb.mantissa - aa.mantissa;
                    res.negative = b.negative;
                }
            }
            res.Normalize();
            if (res.mantissa.IsZero()) res.negative = false;
            return res;
        }

        public static F256 operator -(F256 a, F256 b)
        {
            F256 nb = new F256(b);
            if (a.isNaN || b.isNaN) return new F256("NaN");
            if (a.isInf || b.isInf)
            {
                if (a.isInf && b.isInf) return new F256("NaN");
                if (a.isInf) return a;
                
                nb.negative = !nb.negative;
                return nb;
            }
            nb.negative = !nb.negative;
            return a + nb;
        }

        public static F256 operator *(F256 a, F256 b)
        {
            F256 res = new F256();
            if (a.isNaN || b.isNaN) return new F256("NaN");
            if (a.isInf || b.isInf)
            {
                if (a.IsZero() || b.IsZero()) return new F256("NaN");
                
                res.isInf = true;
                res.negative = a.negative != b.negative;
                return res;
            }
            res.mantissa = a.mantissa * b.mantissa;
            res.exponent = a.exponent + b.exponent;
            res.negative = a.negative != b.negative;
            res.Normalize();
            if (res.mantissa.IsZero()) res.negative = false;
            return res;
        }

        public static F256 operator /(F256 a, F256 b)
        {
            F256 res = new F256();
            if (a.isNaN || b.isNaN || b.IsZero()) return new F256("NaN");
            if (a.isInf || b.isInf)
            {
                if (a.isInf && b.isInf) return new F256("NaN");
                if (a.isInf)
                {
                    res.isInf = true;
                    res.negative = a.negative != b.negative;
                    return res;
                }
                return new F256("0");
            }
            if (a.IsZero()) return new F256("0");

            const int PREC = 40;
            D256 dividend = new D256(a.mantissa);
            D256 ten = new D256(10);
            for (int i = 0; i < PREC; i++)
                dividend = dividend * ten;
            res.mantissa = dividend / b.mantissa;
            res.exponent = a.exponent - b.exponent - PREC;
            res.negative = a.negative != b.negative;
            res.Normalize();
            if (res.mantissa.IsZero()) res.negative = false;
            return res;
        }

        // ----------------------------------------------------------
        // Операторы инкремента/декремента
        // ----------------------------------------------------------
        public static F256 operator ++(F256 a)
        {
            return a + One();
        }

        public static F256 operator --(F256 a)
        {
            return a - One();
        }

        // ----------------------------------------------------------
        // Явное преобразование в double
        // ----------------------------------------------------------
        public static explicit operator double(F256 a)
        {
            if (a.isNaN || a.isInf) return 0.0;
            return double.Parse(a.ToString(), System.Globalization.CultureInfo.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            if (obj is F256 other)
                return this == other;
            return false;
        }
        public override int GetHashCode()
        {
            int h = 0;
            foreach (var w in mantissa.GetWords()) h ^= w.GetHashCode();
            h ^= negative.GetHashCode();
            return h;
        }
    }
}