using System;
using KURSOVAYA;

namespace BigNumbersTest
{
    class Program
    {
        static int totalTests = 0;
        static int passedTests = 0;

        static void Check(bool condition, string name)
        {
            totalTests++;
            if (condition)
            {
                passedTests++;
                Console.WriteLine($"[OK]   {name}");
            }
            else
            {
                Console.WriteLine($"[FAIL] {name}");
            }
        }

        static void CheckStr(string got, string expected, string name)
        {
            totalTests++;
            if (got == expected)
            {
                passedTests++;
                Console.WriteLine($"[OK]   {name} -> {got}");
            }
            else
            {
                Console.WriteLine($"[FAIL] {name}");
                Console.WriteLine($"       got      = {got}");
                Console.WriteLine($"       expected = {expected}");
            }
        }

        static void CheckThrowsD256(string s, string name)
        {
            totalTests++;
            try
            {
                D256 x = new D256(s);
                Console.WriteLine($"[FAIL] {name} (no exception)");
            }
            catch
            {
                passedTests++;
                Console.WriteLine($"[OK]   {name}");
            }
        }

        static void CheckThrowsD512(string s, string name) => CheckThrowsD256(s, name); // аналогично для D512

        static void CheckThrowsF256Div(F256 a, F256 b, string name)
        {
            totalTests++;
            try
            {
                F256 c = a / b;
                if (c.IsNaN())
                {
                    passedTests++;
                    Console.WriteLine($"[OK]   {name}");
                }
                else
                {
                    Console.WriteLine($"[FAIL] {name} (expected NaN)");
                }
            }
            catch
            {
                passedTests++;
                Console.WriteLine($"[OK]   {name} (exception)");
            }
        }

        static void CheckThrowsF512Div(F512 a, F512 b, string name)
        {
            totalTests++;
            try
            {
                F512 c = a / b;
                if (c.IsNaN()) passedTests++;
                else Console.WriteLine($"[FAIL] {name} (expected NaN)");
            }
            catch
            {
                passedTests++;
                Console.WriteLine($"[OK]   {name} (exception)");
            }
        }

        // ----------------------------------------------------------
        // D256 тесты
        // ----------------------------------------------------------
        static void TestD256Constructors()
        {
            Console.WriteLine("\n=== D256 CONSTRUCTORS ===");
            CheckStr(new D256(0).ToString(), "0", "D256(0)");
            CheckStr(new D256(123).ToString(), "123", "D256(123)");
            CheckStr(new D256(-123).ToString(), "-123", "D256(-123)");
            CheckStr(new D256("0").ToString(), "0", "D256(\"0\")");
            CheckStr(new D256("123456").ToString(), "123456", "D256(\"123456\")");
            CheckStr(new D256("-987654").ToString(), "-987654", "D256(\"-987654\")");
        }

        static void TestD256InvalidStrings()
        {
            Console.WriteLine("\n=== D256 INVALID STRINGS ===");
            CheckThrowsD256("", "empty string");
            CheckThrowsD256("-", "minus only");
            CheckThrowsD256("00", "leading zero 00");
            CheckThrowsD256("0123", "leading zero 0123");
            CheckThrowsD256("abc", "letters");
            CheckThrowsD256("12a3", "mixed alnum");
            CheckThrowsD256("--1", "double minus");
            CheckThrowsD256("1-2", "minus inside");
        }

        static void TestD256Comparisons()
        {
            Console.WriteLine("\n=== D256 COMPARISONS ===");
            D256 a = new D256("123");
            D256 b = new D256("123");
            D256 c = new D256("124");
            D256 d = new D256("-123");
            D256 z = new D256("0");
            Check(a == b, "123 == 123");
            Check(a != c, "123 != 124");
            Check(a < c, "123 < 124");
            Check(c > a, "124 > 123");
            Check(d < a, "-123 < 123");
            Check(d < z, "-123 < 0");
            Check(z < a, "0 < 123");
            Check(!(a < b), "123 !< 123");
        }

        static void TestD256ArithmeticBasic()
        {
            Console.WriteLine("\n=== D256 BASIC ARITHMETIC ===");
            CheckStr((new D256("123") + new D256("456")).ToString(), "579", "123 + 456");
            CheckStr((new D256("1000") - new D256("1")).ToString(), "999", "1000 - 1");
            CheckStr((new D256("12") * new D256("34")).ToString(), "408", "12 * 34");
            CheckStr((new D256("100") / new D256("4")).ToString(), "25", "100 / 4");
            CheckStr((new D256("100") % new D256("6")).ToString(), "4", "100 % 6");
        }

        static void TestD256ArithmeticSigns()
        {
            Console.WriteLine("\n=== D256 SIGN ARITHMETIC ===");
            CheckStr((new D256("-5") + new D256("3")).ToString(), "-2", "-5 + 3");
            CheckStr((new D256("5") + new D256("-3")).ToString(), "2", "5 + (-3)");
            CheckStr((new D256("-5") + new D256("-3")).ToString(), "-8", "-5 + (-3)");

            CheckStr((new D256("5") - new D256("3")).ToString(), "2", "5 - 3");
            CheckStr((new D256("3") - new D256("5")).ToString(), "-2", "3 - 5");
            CheckStr((new D256("-5") - new D256("3")).ToString(), "-8", "-5 - 3");
            CheckStr((new D256("5") - new D256("-3")).ToString(), "8", "5 - (-3)");

            CheckStr((new D256("-6") * new D256("7")).ToString(), "-42", "-6 * 7");
            CheckStr((new D256("-6") * new D256("-7")).ToString(), "42", "-6 * -7");

            CheckStr((new D256("-20") / new D256("5")).ToString(), "-4", "-20 / 5");
            CheckStr((new D256("20") / new D256("-5")).ToString(), "-4", "20 / -5");
            CheckStr((new D256("-20") / new D256("-5")).ToString(), "4", "-20 / -5");
        }

        static void TestD256DivisionEdgeCases()
        {
            Console.WriteLine("\n=== D256 DIVISION EDGE CASES ===");
            CheckStr((new D256("0") / new D256("5")).ToString(), "0", "0 / 5");
            CheckStr((new D256("5") / new D256("10")).ToString(), "0", "5 / 10");
            CheckStr((new D256("5") % new D256("10")).ToString(), "5", "5 % 10");
            CheckStr((new D256("999") / new D256("1")).ToString(), "999", "999 / 1");
            CheckStr((new D256("999") % new D256("1")).ToString(), "0", "999 % 1");
            CheckStr((new D256("999") / new D256("999")).ToString(), "1", "999 / 999");
            CheckStr((new D256("999") % new D256("999")).ToString(), "0", "999 % 999");
        }

        static void TestD256BigNumbers()
        {
            Console.WriteLine("\n=== D256 BIG NUMBERS ===");
            D256 a = new D256("1234567890123456789012345678901234567890");
            D256 b = new D256("9876543210987654321098765432109876543210");
            CheckStr((a + b).ToString(), "11111111101111111110111111111011111111100", "big add");
            CheckStr((b - a).ToString(), "8641975320864197532086419753208641975320", "big sub");
        }

        static void TestD256SpecialCases()
        {
            Console.WriteLine("\n=== D256 SPECIAL CASES ===");
            D256 z = new D256("0");
            D256 one = new D256("1");
            D256 minusOne = new D256("-1");
            Check(z.IsZero(), "zero isZero");
            Check(one.IsOne(), "one isOne");
            Check(!minusOne.IsOne(), "-1 is not one");
            D256 zzzz = new D256();
            CheckStr((++zzzz).ToString(), "1", "++0");
            D256 zzz = new D256(1);
            CheckStr((--zzz).ToString(), "0", "0");
            CheckStr((zzz).ToString(), "0", "-1");
        }

        static void TestD512Constructors()
        {
            Console.WriteLine("\n=== D512 CONSTRUCTORS ===");
            CheckStr(new D512(0).ToString(), "0", "D512(0)");
            CheckStr(new D512(123).ToString(), "123", "D512(123)");
            CheckStr(new D512(-123).ToString(), "-123", "D512(-123)");
            CheckStr(new D512("0").ToString(), "0", "D512(\"0\")");
            CheckStr(new D512("123456").ToString(), "123456", "D512(\"123456\")");
            CheckStr(new D512("-987654").ToString(), "-987654", "D512(\"-987654\")");
        }

        static void TestD512Comparisons()
        {
            Console.WriteLine("\n=== D512 COMPARISONS ===");
            D512 a = new D512("123");
            D512 b = new D512("123");
            D512 c = new D512("124");
            D512 d = new D512("-123");
            D512 z = new D512("0");
            Check(a == b, "123 == 123");
            Check(a != c, "123 != 124");
            Check(a < c, "123 < 124");
            Check(c > a, "124 > 123");
            Check(d < a, "-123 < 123");
            Check(d < z, "-123 < 0");
            Check(z < a, "0 < 123");
            Check(!(a < b), "123 !< 123");
        }

        static void TestD512ArithmeticBasic()
        {
            Console.WriteLine("\n=== D512 BASIC ARITHMETIC ===");
            CheckStr((new D512("123") + new D512("456")).ToString(), "579", "123 + 456");
            CheckStr((new D512("1000") - new D512("1")).ToString(), "999", "1000 - 1");
            CheckStr((new D512("12") * new D512("34")).ToString(), "408", "12 * 34");
            CheckStr((new D512("100") / new D512("4")).ToString(), "25", "100 / 4");
            CheckStr((new D512("100") % new D512("6")).ToString(), "4", "100 % 6");
        }

        static void TestD512DivisionEdgeCases()
        {
            Console.WriteLine("\n=== D512 DIVISION EDGE CASES ===");
            CheckStr((new D512("0") / new D512("5")).ToString(), "0", "0 / 5");
            CheckStr((new D512("5") / new D512("10")).ToString(), "0", "5 / 10");
            CheckStr((new D512("5") % new D512("10")).ToString(), "5", "5 % 10");
            CheckStr((new D512("999") / new D512("1")).ToString(), "999", "999 / 1");
            CheckStr((new D512("999") % new D512("1")).ToString(), "0", "999 % 1");
            CheckStr((new D512("999") / new D512("999")).ToString(), "1", "999 / 999");
            CheckStr((new D512("999") % new D512("999")).ToString(), "0", "999 % 999");
        }

        static void TestD512BigNumbers()
        {
            Console.WriteLine("\n=== D512 BIG NUMBERS ===");
            D512 a = new D512("123456789012345678901234567890");
            D512 b = new D512("987654321098765432109876543210");
            CheckStr((a + b).ToString(), "1111111110111111111011111111100", "big add");
            CheckStr((b - a).ToString(), "864197532086419753208641975320", "big sub");
        }
        static void TestF256Constructors()
        {
            Console.WriteLine("\n=== F256 CONSTRUCTORS ===");
            CheckStr(new F256("0").ToString(), "0", "F256(\"0\")");
            CheckStr(new F256("123.456").ToString(), "123.456", "F256(\"123.456\")");
            CheckStr(new F256("-0.001").ToString(), "-0.001", "F256(\"-0.001\")");
            CheckStr(new F256("1e3").ToString(), "1000", "F256(\"1e3\")");
            CheckStr(new F256("1e-3").ToString(), "0.001", "F256(\"1e-3\")");
            CheckStr(new F256("-2.5e2").ToString(), "-250", "F256(\"-2.5e2\")");
        }

        static void TestF256Comparisons()
        {
            Console.WriteLine("\n=== F256 COMPARISONS ===");
            F256 a = new F256("1.5");
            F256 b = new F256("1.50");
            F256 c = new F256("1.5001");
            F256 d = new F256("-1.5");
            F256 z = new F256("0");
            Check(a == b, "1.5 == 1.50");
            Check(a != c, "1.5 != 1.5001");
            Check(a < c, "1.5 < 1.5001");
            Check(c > a, "1.5001 > 1.5");
            Check(d < z, "-1.5 < 0");
            Check(z < a, "0 < 1.5");
            Check(d < a, "-1.5 < 1.5");
        }

        static void TestF256ArithmeticBasic()
        {
            Console.WriteLine("\n=== F256 BASIC ARITHMETIC ===");
            CheckStr((new F256("123.456") + new F256("0.001")).ToString(), "123.457", "123.456 + 0.001");
            CheckStr((new F256("123.456") - new F256("0.001")).ToString(), "123.455", "123.456 - 0.001");
            CheckStr((new F256("1.5") * new F256("2.5")).ToString(), "3.75", "1.5 * 2.5");
            CheckStr((new F256("10") / new F256("4")).ToString(), "2.5", "10 / 4");
        }

        static void TestF256Signs()
        {
            Console.WriteLine("\n=== F256 SIGN ARITHMETIC ===");
            CheckStr((new F256("-5.5") + new F256("2.5")).ToString(), "-3", "-5.5 + 2.5");
            CheckStr((new F256("5.5") + new F256("-2.5")).ToString(), "3", "5.5 + (-2.5)");
            CheckStr((new F256("-5.5") - new F256("2.5")).ToString(), "-8", "-5.5 - 2.5");
            CheckStr((new F256("5.5") - new F256("-2.5")).ToString(), "8", "5.5 - (-2.5)");

            CheckStr((new F256("-1.5") * new F256("2")).ToString(), "-3", "-1.5 * 2");
            CheckStr((new F256("-1.5") * new F256("-2")).ToString(), "3", "-1.5 * -2");

            CheckStr((new F256("-10") / new F256("4")).ToString(), "-2.5", "-10 / 4");
            CheckStr((new F256("10") / new F256("-4")).ToString(), "-2.5", "10 / -4");
        }

        static void TestF256Precision()
        {
            Console.WriteLine("\n=== F256 PRECISION ===");
            F256 a = new F256("0.1");
            F256 b = new F256("0.2");
            F256 c = new F256("0.3");
            Check((a + b) == c, "0.1 + 0.2 == 0.3");
            Check((c - a) == b, "0.3 - 0.1 == 0.2");

            F256 x = new F256("1231.123");
            string small = "0.";
            for (int i = 0; i < 70; i++) small += "1";
            F256 y = new F256(small);
            F256 sum = x + y;
            F256 back = sum - x;
            Check(back == y, "(x + y) - x == y");
        }

        static void TestF256ExtremePrecision()
        {
            Console.WriteLine("\n=== F256 EXTREME PRECISION ===");
            string tinyStr = "0.";
            for (int i = 0; i < 80; i++) tinyStr += "0";
            tinyStr += "1";
            F256 tiny = new F256(tinyStr);
            F256 one = new F256("1");
            F256 sum = tiny + one;
            F256 diff = sum - one;
            Check(!(sum > one), "tiny + 1 > 1 is false");
        }

        static void TestF256ScientificNotation()
        {
            Console.WriteLine("\n=== F256 SCIENTIFIC NOTATION ===");
            CheckStr(new F256("1e0").ToString(), "1", "1e0");
            CheckStr(new F256("1e1").ToString(), "10", "1e1");
            CheckStr(new F256("1e-1").ToString(), "0.1", "1e-1");
            CheckStr(new F256("3.1415e2").ToString(), "314.15", "3.1415e2");
            CheckStr(new F256("3.1415e-2").ToString(), "0.031415", "3.1415e-2");
        }

        static void TestF256SpecialValues()
        {
            Console.WriteLine("\n=== F256 SPECIAL VALUES ===");
            F256 nan = new F256("NaN");
            F256 inf = new F256("inf");
            F256 ninf = new F256("-inf");
            F256 one = new F256("1");
            F256 zero = new F256("0");
            Check(nan.IsNaN(), "NaN flag");
            Check(inf.IsInfinity(), "inf flag");
            Check(ninf.IsInfinity(), "-inf flag");
            Check(inf > one, "inf > 1");
            Check(ninf < zero, "-inf < 0");
        }

        static void TestF256DivisionSpecialCases()
        {
            Console.WriteLine("\n=== F256 DIVISION SPECIAL CASES ===");
            CheckStr((new F256("0") / new F256("5")).ToString(), "0", "0 / 5");
            CheckThrowsF256Div(new F256("5"), new F256("0"), "5 / 0 -> NaN");
        }

        static void TestF256RoundTrip()
        {
            Console.WriteLine("\n=== F256 ROUND-TRIP TESTS ===");
            F256 a = new F256("9999.125");
            F256 b = new F256("0.875");
            F256 c = a + b;
            F256 d = c - b;
            Check(d == a, "(a + b) - b == a");
            F256 e = new F256("12.5");
            F256 f = new F256("2");
            F256 g = e * f;
            F256 h = g / f;
            Check(h == e, "(e * f) / f == e");
        }

        static void EvilTestF256()
        {
            Console.WriteLine("\n=== EVIL TEST (F256) ===");
            string maxStr = "115792089237316195423570985008687907853269984665640564039457584007913129639935";
            F256 a = new F256(maxStr);
            F256 b = new F256("0.0000000000000000000000000000000000000000000000000000000000000000000000000001");
            Console.WriteLine($"a = {a}");
            Console.WriteLine($"b = {b}");
            Console.WriteLine($"a + b = {a + b}");
            Console.WriteLine($"a - b = {a - b}");
            Console.WriteLine($"a * b = {a * b}");
            Console.WriteLine($"a / b = {a / b}");
        }

        static void TestF512Constructors()
        {
            Console.WriteLine("\n=== F512 CONSTRUCTORS ===");
            CheckStr(new F512("0").ToString(), "0", "F512(\"0\")");
            CheckStr(new F512("123.456").ToString(), "123.456", "F512(\"123.456\")");
            CheckStr(new F512("-0.001").ToString(), "-0.001", "F512(\"-0.001\")");
            CheckStr(new F512("1e3").ToString(), "1000", "F512(\"1e3\")");
            CheckStr(new F512("1e-3").ToString(), "0.001", "F512(\"1e-3\")");
            CheckStr(new F512("-2.5e2").ToString(), "-250", "F512(\"-2.5e2\")");
        }

        static void TestF512Comparisons()
        {
            Console.WriteLine("\n=== F512 COMPARISONS ===");
            F512 a = new F512("1.5");
            F512 b = new F512("1.50");
            F512 c = new F512("1.5001");
            F512 d = new F512("-1.5");
            F512 z = new F512("0");
            Check(a == b, "1.5 == 1.50");
            Check(a != c, "1.5 != 1.5001");
            Check(a < c, "1.5 < 1.5001");
            Check(c > a, "1.5001 > 1.5");
            Check(d < z, "-1.5 < 0");
            Check(z < a, "0 < 1.5");
            Check(d < a, "-1.5 < 1.5");
        }

        static void TestF512ArithmeticBasic()
        {
            Console.WriteLine("\n=== F512 BASIC ARITHMETIC ===");
            CheckStr((new F512("123.456") + new F512("0.001")).ToString(), "123.457", "123.456 + 0.001");
            CheckStr((new F512("123.456") - new F512("0.001")).ToString(), "123.455", "123.456 - 0.001");
            CheckStr((new F512("1.5") * new F512("2.5")).ToString(), "3.75", "1.5 * 2.5");
            CheckStr((new F512("10") / new F512("4")).ToString(), "2.5", "10 / 4");
        }

        static void TestF512Precision()
        {
            Console.WriteLine("\n=== F512 PRECISION ===");
            F512 a = new F512("0.1");
            F512 b = new F512("0.2");
            F512 c = new F512("0.3");
            Check((a + b) == c, "0.1 + 0.2 == 0.3");
            Check((c - a) == b, "0.3 - 0.1 == 0.2");
            F512 x = new F512("1231.123");
            F512 y = new F512("0.111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111");
            F512 sum = x + y;
            F512 back = sum - x;
            Check(back == y, "(x + y) - x == y");
        }

        static void TestF512ExtremePrecision()
        {
            Console.WriteLine("\n=== F512 EXTREME PRECISION ===");
            string tinyStr = "0.";
            for (int i = 0; i < 200; i++) tinyStr += "0";
            tinyStr += "1";
            F512 tiny = new F512(tinyStr);
            F512 one = new F512("1");
            F512 sum = tiny + one;
            F512 diff = sum - one;
            Check(!diff.IsZero(), "(tiny + 1) - 1 == 0");
            Check(!(sum > one), "tiny + 1 > 1 is false");
        }

        static void EvilTestF512()
        {
            Console.WriteLine("\n=== EVIL TEST (F512) ===");
            F512 a = new F512("9.99999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999");
            F512 b = new F512("0.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001");
            Console.WriteLine($"a = {a}");
            Console.WriteLine($"b = {b}");
            Console.WriteLine($"a + b = {a + b}");
            Console.WriteLine($"a - b = {a - b}");
            Console.WriteLine($"a * b = {a * b}");
        }
        static void Main(string[] args)
        {
            try
            {
                // D256
                TestD256Constructors();
                TestD256InvalidStrings();
                TestD256Comparisons();
                TestD256ArithmeticBasic();
                TestD256ArithmeticSigns();
                TestD256DivisionEdgeCases();
                TestD256BigNumbers();
                TestD256SpecialCases();

                // D512
                TestD512Constructors();
                TestD512Comparisons();
                TestD512ArithmeticBasic();
                TestD512DivisionEdgeCases();
                TestD512BigNumbers();

                // F256
                TestF256Constructors();
                TestF256Comparisons();
                TestF256ArithmeticBasic();
                TestF256Signs();
                TestF256Precision();
                TestF256ExtremePrecision();
                TestF256ScientificNotation();
                TestF256SpecialValues();
                TestF256DivisionSpecialCases();
                TestF256RoundTrip();
                EvilTestF256();

                // F512
                TestF512Constructors();
                TestF512Comparisons();
                TestF512ArithmeticBasic();
                TestF512Precision();
                TestF512ExtremePrecision();
                EvilTestF512();

                Console.WriteLine("\n========================================");
                Console.WriteLine($"PASSED: {passedTests} / {totalTests}");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUNHANDLED EXCEPTION: {ex.Message}");
            }
        }
    }
}