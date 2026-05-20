using KURSOVAYA;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace KURSOVAYA_APP
{
    public partial class Form1 : Form
    {
        enum InputMode { HEX, DEC, OCT, BIN_GRID, BIN_KEYBOARD }
        enum Operation { NONE, ADD, SUB, MUL, DIV, AND, OR, XOR, SHL, SHR }

        InputMode currentMode = InputMode.BIN_GRID;
        Operation currentOp = Operation.NONE;

        D512 number = new D512(0);
        D512 storedNumber = new D512(0);
        F512 floatNumber = new F512("0");
        F512 storedFloat = new F512("0");

        string currentInput = "";
        string outputText = "";
        bool hasResult = false;
        bool isFloat = false;
        bool leftIsFloat = false;

        Rectangle hexBtn, decBtn, octBtn, binBtn;
        Rectangle addBtn, subBtn, mulBtn, divBtn;
        Rectangle andBtn, orBtn, xorBtn, notBtn;
        Rectangle shlBtn, shrBtn;
        Rectangle clearBtn, backspaceBtn, equalsBtn;
        Rectangle dotBtn, signBtn, copyBtn;
        Rectangle[] numButtons = new Rectangle[16];

        Rectangle outputField;
        Rectangle inputField;
        Rectangle basesField;

        Timer animationTimer;
        private Panel mainPanel;

        private VScrollBar outputScrollBar;
        private int outputScrollOffset = 0;

        private VScrollBar inputScrollBar;
        private int inputScrollOffset = 0;
        private bool scrollToBottomInput = false;

        private VScrollBar basesScrollBar;
        private int basesScrollOffset = 0;

        public Form1()
        {
            InitializeComponent();
            this.Text = "Calculator";
            //this.Icon = new Icon("icon.ico");
            this.ClientSize = new Size(630, 780);
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.BackColor = Color.FromArgb(20, 20, 20);
            mainPanel.Paint += Panel_Paint;
            mainPanel.MouseClick += Panel_MouseClick;
            mainPanel.MouseWheel += Panel_MouseWheel;

            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(mainPanel, true, null);

            outputScrollBar = new VScrollBar() { SmallChange = 15, LargeChange = 45, Visible = false };
            outputScrollBar.Scroll += (s, ev) => { outputScrollOffset = ev.NewValue; mainPanel.Invalidate(); };

            inputScrollBar = new VScrollBar() { SmallChange = 15, LargeChange = 45, Visible = false };
            inputScrollBar.Scroll += (s, ev) => { inputScrollOffset = ev.NewValue; mainPanel.Invalidate(); };

            basesScrollBar = new VScrollBar() { SmallChange = 15, LargeChange = 45, Visible = false };
            basesScrollBar.Scroll += (s, ev) => { basesScrollOffset = ev.NewValue; mainPanel.Invalidate(); };

            mainPanel.Controls.Add(outputScrollBar);
            mainPanel.Controls.Add(inputScrollBar);
            mainPanel.Controls.Add(basesScrollBar);

            this.Controls.Add(mainPanel);

            animationTimer = new Timer();
            animationTimer.Interval = 500;
            animationTimer.Tick += (s, e) => { mainPanel?.Invalidate(); };
            animationTimer.Start();

            this.KeyDown += Form1_KeyDown;
        }

        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.FromArgb(20, 20, 20));
            int w = ((Panel)sender).Width;

            DrawTitle(g, w);
            DrawOutputField(g, w);
            DrawInputField(g, w);
            DrawAllBases(g, w);
            DrawModeButtons(g);
            DrawOperationButtons(g);
            DrawKeyboard(g);
            DrawBitGrid(g);
        }

        private void DrawTitle(Graphics g, int w)
        {
            using (Font f = new Font("Consolas", 14, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 200, 0)))
            {
                string t = "CALCULATOR";
                float x = (w - g.MeasureString(t, f).Width) / 2;
                g.DrawString(t, f, brush, x, 3);
            }
        }

        private void DrawOutputField(Graphics g, int w)
        {
            outputField = new Rectangle(20, 25, w - 40, 80);

            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(35, 35, 35)))
            {
                g.FillRectangle(bgBrush, outputField);
            }

            string text = hasResult ? outputText : "_";
            using (Font f = new Font("Consolas", 9, FontStyle.Bold))
            {
                int aw = outputField.Width - (outputScrollBar.Visible ? 25 : 10);
                SizeF ts = g.MeasureString(text, f, aw);
                int ms = Math.Max(0, (int)ts.Height - outputField.Height + 10);

                if (ms > 0)
                {
                    outputScrollBar.Location = new Point(outputField.Right - 20, outputField.Top + 1);
                    outputScrollBar.Size = new Size(20, outputField.Height - 2);
                    outputScrollBar.Maximum = ms + outputScrollBar.LargeChange - 1;
                    outputScrollBar.Visible = true;
                    if (outputScrollOffset > ms) outputScrollOffset = ms;
                }
                else
                {
                    outputScrollBar.Visible = false;
                    outputScrollOffset = 0;
                    outputScrollBar.Value = 0;
                }

                // Отсечение текста, чтобы он не вылезал за границы!
                g.SetClip(outputField);

                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(0, 220, 220)))
                {
                    g.DrawString(text, f, textBrush,
                        new RectangleF(outputField.X + 5, outputField.Y + 5 - outputScrollOffset, aw, ts.Height + 100));
                }

                g.ResetClip();
            }

            using (Pen borderPen = new Pen(Color.FromArgb(0, 180, 180), 1))
            {
                g.DrawRectangle(borderPen, outputField);
            }

            copyBtn = new Rectangle(outputField.Right - 45, outputField.Top - 20, 45, 20);
            DrawStyledButton(g, copyBtn, "COPY", false, Color.FromArgb(0, 100, 150), hasResult);
        }

        private void DrawInputField(Graphics g, int w)
        {
            inputField = new Rectangle(20, 110, w - 40, 80);

            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
            {
                g.FillRectangle(bgBrush, inputField);
            }

            string text;
            if (currentOp != Operation.NONE)
            {
                string op = GetOperationSymbol(currentOp);
                string left = leftIsFloat ? storedFloat.ToString() : FormatNumberInCurrentMode(storedNumber);
                text = $"{left} {op} {currentInput}";
            }
            else
            {
                text = string.IsNullOrEmpty(currentInput) ? "_" : currentInput;
            }

            using (Font f = new Font("Consolas", 9, FontStyle.Bold))
            {
                bool cur = DateTime.Now.Millisecond < 500;
                if (cur) text += "|";

                int aw = inputField.Width - (inputScrollBar.Visible ? 25 : 10);
                SizeF ts = g.MeasureString(text, f, aw);
                int ms = Math.Max(0, (int)ts.Height - inputField.Height + 10);

                if (ms > 0)
                {
                    inputScrollBar.Location = new Point(inputField.Right - 20, inputField.Top + 1);
                    inputScrollBar.Size = new Size(20, inputField.Height - 2);
                    inputScrollBar.Maximum = ms + inputScrollBar.LargeChange - 1;
                    inputScrollBar.Visible = true;

                    if (scrollToBottomInput)
                    {
                        inputScrollOffset = ms;
                        inputScrollBar.Value = ms;
                        scrollToBottomInput = false;
                    }
                    else if (inputScrollOffset > ms)
                    {
                        inputScrollOffset = ms;
                    }
                }
                else
                {
                    inputScrollBar.Visible = false;
                    inputScrollOffset = 0;
                    inputScrollBar.Value = 0;
                }

                // Отсечение текста
                g.SetClip(inputField);
                g.DrawString(text, f, Brushes.White,
                    new RectangleF(inputField.X + 5, inputField.Y + 5 - inputScrollOffset, aw, ts.Height + 100));
                g.ResetClip();
            }

            Color bc = currentOp != Operation.NONE ? Color.FromArgb(255, 200, 0) : Color.FromArgb(0, 200, 0);
            using (Pen borderPen = new Pen(bc, 1))
            {
                g.DrawRectangle(borderPen, inputField);
            }
        }

        private void DrawAllBases(Graphics g, int w)
        {
            basesField = new Rectangle(20, 195, w - 40, 65);

            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(25, 25, 25)))
            {
                g.FillRectangle(bgBrush, basesField);
            }

            string hexStr = number.ToBase(16);
            string decStr = isFloat ? floatNumber.ToString() : number.ToString();
            string octStr = number.ToBase(8);
            string binStr = number.ToBase(2).PadLeft(512, '0');

            string text = $"HEX: {hexStr}\n\nDEC: {decStr}\n\nOCT: {octStr}\n\nBIN: {binStr}";

            using (Font f = new Font("Consolas", 8, FontStyle.Bold))
            {
                int aw = basesField.Width - (basesScrollBar.Visible ? 25 : 10);
                SizeF ts = g.MeasureString(text, f, aw);
                int ms = Math.Max(0, (int)ts.Height - basesField.Height + 10);

                if (ms > 0)
                {
                    basesScrollBar.Location = new Point(basesField.Right - 20, basesField.Top + 1);
                    basesScrollBar.Size = new Size(20, basesField.Height - 2);
                    basesScrollBar.Maximum = ms + basesScrollBar.LargeChange - 1;
                    basesScrollBar.Visible = true;
                    if (basesScrollOffset > ms) basesScrollOffset = ms;
                }
                else
                {
                    basesScrollBar.Visible = false;
                    basesScrollOffset = 0;
                    basesScrollBar.Value = 0;
                }

                // Отсечение текста
                g.SetClip(basesField);
                g.DrawString(text, f, Brushes.LightGray,
                    new RectangleF(basesField.X + 5, basesField.Y + 5 - basesScrollOffset, aw, ts.Height + 100));
                g.ResetClip();
            }

            using (Pen borderPen = new Pen(Color.FromArgb(50, 50, 50), 1))
            {
                g.DrawRectangle(borderPen, basesField);
            }
        }

        private void DrawModeButtons(Graphics g)
        {
            int y = 265;
            hexBtn = new Rectangle(20, y, 90, 30);
            decBtn = new Rectangle(118, y, 90, 30);
            octBtn = new Rectangle(216, y, 90, 30);
            binBtn = new Rectangle(314, y, 90, 30);

            DrawStyledButton(g, hexBtn, "HEX", currentMode == InputMode.HEX, Color.FromArgb(0, 150, 0), !isFloat);
            DrawStyledButton(g, decBtn, "DEC", currentMode == InputMode.DEC, Color.FromArgb(0, 100, 200), true);
            DrawStyledButton(g, octBtn, "OCT", currentMode == InputMode.OCT, Color.FromArgb(200, 150, 0), !isFloat);

            string btxt = currentMode == InputMode.BIN_GRID ? "BIN GRID" : currentMode == InputMode.BIN_KEYBOARD ? "BIN KEY" : "BIN";
            DrawStyledButton(g, binBtn, btxt, currentMode == InputMode.BIN_GRID || currentMode == InputMode.BIN_KEYBOARD, Color.FromArgb(150, 0, 200), !isFloat);
        }

        private void DrawKeyboard(Graphics g)
        {
            string[] keys = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F" };
            for (int i = 0; i < 16; i++)
            {
                int row = i / 4, col = i % 4;
                Rectangle r = new Rectangle(430 + col * 43, 265 + row * 43, 40, 40);
                numButtons[i] = r;
                bool vis = IsKeyValid(keys[i][0]);

                if (vis)
                    DrawStyledButton(g, r, keys[i], false, Color.FromArgb(50, 50, 50), true);
                else if (currentMode != InputMode.BIN_GRID && currentMode != InputMode.BIN_KEYBOARD)
                    DrawStyledButton(g, r, keys[i], false, Color.FromArgb(30, 30, 30), false);
            }
        }

        private void DrawOperationButtons(Graphics g)
        {
            int y = 305;
            var ops = new (string t, Operation op, Color c)[]
            {
                ("+", Operation.ADD, Color.FromArgb(60,60,60)), ("-", Operation.SUB, Color.FromArgb(60,60,60)),
                ("×", Operation.MUL, Color.FromArgb(60,60,60)), ("÷", Operation.DIV, Color.FromArgb(60,60,60)),
                ("&", Operation.AND, Color.FromArgb(60,60,60)), ("|", Operation.OR, Color.FromArgb(60,60,60)),
                ("^", Operation.XOR, Color.FromArgb(60,60,60)), ("~", Operation.NONE, Color.FromArgb(60,60,60)),
                ("<<", Operation.SHL, Color.FromArgb(60,60,60)), (">>", Operation.SHR, Color.FromArgb(60,60,60)),
                (".", Operation.NONE, Color.FromArgb(60,60,60)), ("+/-", Operation.NONE, Color.FromArgb(60,60,60)),
                ("CLR", Operation.NONE, Color.FromArgb(200,50,50)), ("⌫", Operation.NONE, Color.FromArgb(200,100,0)),
                ("=", Operation.NONE, Color.FromArgb(0,150,0))
            };

            for (int i = 0; i < ops.Length; i++)
            {
                int col = i % 7, row = i / 7;
                Rectangle r = new Rectangle(20 + col * 43, y + row * 43, 40, 40);

                bool blocked = IsOperationBlocked(ops[i].op);
                if (ops[i].t == "." && currentMode != InputMode.DEC) blocked = true;

                DrawStyledButton(g, r, ops[i].t, ops[i].op != Operation.NONE && currentOp == ops[i].op, ops[i].c, !blocked);

                switch (ops[i].t)
                {
                    case "CLR": clearBtn = r; break;
                    case "⌫": backspaceBtn = r; break;
                    case "=": equalsBtn = r; break;
                    case "+": addBtn = r; break;
                    case "-": subBtn = r; break;
                    case "×": mulBtn = r; break;
                    case "÷": divBtn = r; break;
                    case "&": andBtn = r; break;
                    case "|": orBtn = r; break;
                    case "^": xorBtn = r; break;
                    case "~": notBtn = r; break;
                    case "<<": shlBtn = r; break;
                    case ">>": shrBtn = r; break;
                    case ".": dotBtn = r; break;
                    case "+/-": signBtn = r; break;
                }
            }
        }

        private bool IsOperationBlocked(Operation op)
        {
            if (op == Operation.NONE) return false;
            if ((isFloat || leftIsFloat) && IsBitwiseOperation(op)) return true;
            return false;
        }

        private void DrawBitGrid(Graphics g)
        {
            int startY = 450, cs = 16, gx = 2, gy = 4, gg = 8, sx = 20;
            bool isActive = (currentMode == InputMode.BIN_GRID); // Сетка активна только в BIN_GRID

            using (Font bf = new Font("Consolas", 6, FontStyle.Bold))
            using (Font inf = new Font("Consolas", 6))
            using (SolidBrush onBrush = new SolidBrush(isActive ? Color.FromArgb(50, 200, 50) : Color.FromArgb(60, 60, 60)))
            using (SolidBrush offBrush = new SolidBrush(isActive ? Color.FromArgb(45, 45, 45) : Color.FromArgb(35, 35, 35)))
            using (Pen borderPen = new Pen(isActive ? Color.FromArgb(60, 60, 60) : Color.FromArgb(40, 40, 40)))
            {
                StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // Группы
                for (int gi = 0; gi < 4; gi++)
                {
                    int gpx = sx + gi * 8 * (cs + gx) + gi * gg;
                    Color groupColor = isActive ? Color.Orange : Color.FromArgb(60, 60, 60);
                    g.DrawString($"B{40 - 8*(gi+1)}", inf, new SolidBrush(groupColor), gpx + 4 * cs - 8, startY - 12);
                }

                // Биты
                for (int r = 0; r < 16; r++)
                {
                    Color rowColor = isActive ? Color.White : Color.FromArgb(60, 60, 60);
                    g.DrawString((16 - r).ToString(), inf, new SolidBrush(rowColor), 5, startY + r * (cs + gy) + 2);

                    for (int c = 0; c < 32; c++)
                    {
                        int idx = 511 - (r * 32 + c);
                        int x = sx + c * (cs + gx) + (c / 8) * gg;
                        int y = startY + r * (cs + gy);
                        Rectangle rect = new Rectangle(x, y, cs, cs);

                        bool bit = number.GetBit((uint)idx);

                        g.FillRectangle(bit ? onBrush : offBrush, rect);
                        g.DrawRectangle(borderPen, rect);

                        Color textColor = isActive ? (bit ? Color.Black : Color.White) : Color.FromArgb(60, 60, 60);
                        g.DrawString(bit ? "1" : "0", bf, new SolidBrush(textColor), rect, sf);
                    }
                }
            }
        }

        private void DrawStyledButton(Graphics g, Rectangle r, string t, bool active, Color bc, bool enabled)
        {
            Color bg = !enabled ? Color.FromArgb(30, 30, 30) : active ? bc : Color.FromArgb(50, 50, 50);
            Color border = !enabled ? Color.FromArgb(40, 40, 40) : active ? Color.FromArgb(0, 200, 0) : Color.FromArgb(80, 80, 80);
            Color textColor = enabled ? Color.White : Color.FromArgb(60, 60, 60);

            using (SolidBrush bgBrush = new SolidBrush(bg))
            using (Pen pen = new Pen(border, active ? 2 : 1))
            using (Font f = new Font("Consolas", 8, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                g.FillRectangle(bgBrush, r);
                g.DrawRectangle(pen, r);

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(t, f, textBrush, r, sf);
            }
        }

        private bool IsKeyValid(char key)
        {
            switch (currentMode)
            {
                case InputMode.HEX: return true;
                case InputMode.DEC: return (key >= '0' && key <= '9') || key == '.';
                case InputMode.OCT: return key >= '0' && key <= '7';
                case InputMode.BIN_KEYBOARD:
                case InputMode.BIN_GRID: return key == '0' || key == '1';
                default: return false;
            }
        }

        private string FormatNumberInCurrentMode(D512 num)
        {
            switch (currentMode)
            {
                case InputMode.HEX: return num.ToBase(16);
                case InputMode.DEC: return num.ToString();
                case InputMode.OCT: return num.ToBase(8);
                case InputMode.BIN_KEYBOARD:
                case InputMode.BIN_GRID: return num.ToBase(2);
                default: return num.ToBase(16);
            }
        }

        private string GetOperationSymbol(Operation op)
        {
            switch (op)
            {
                case Operation.ADD: return "+";
                case Operation.SUB: return "-";
                case Operation.MUL: return "×";
                case Operation.DIV: return "÷";
                case Operation.AND: return "&";
                case Operation.OR: return "|";
                case Operation.XOR: return "^";
                case Operation.SHL: return "<<";
                case Operation.SHR: return ">>";
                default: return "?";
            }
        }

        private int GetCurrentBase()
        {
            switch (currentMode)
            {
                case InputMode.HEX: return 16;
                case InputMode.DEC: return 10;
                case InputMode.OCT: return 8;
                case InputMode.BIN_KEYBOARD:
                case InputMode.BIN_GRID: return 2;
                default: return 10;
            }
        }

        private void ProcessInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            input = input.ToUpperInvariant();

            if (input == ".") { ProcessDot(); return; }
            if (!IsKeyValid(input[0])) return;

            BeginNewInputAfterResult();
            currentInput += input;
            isFloat = currentInput.Contains(".");
            scrollToBottomInput = true;

            UpdateNumberFromInput();
            mainPanel.Invalidate();
        }

        private void UpdateNumberFromInput()
        {
            if (string.IsNullOrEmpty(currentInput))
            {
                number = new D512(0);
                floatNumber = new F512("0");
                isFloat = false;
                return;
            }

            if (currentMode == InputMode.DEC)
            {
                try
                {
                    floatNumber = new F512(currentInput);
                    isFloat = currentInput.Contains(".");
                    if (isFloat)
                    {
                        string[] parts = currentInput.TrimStart('-').Split('.');
                        string intPart = string.IsNullOrEmpty(parts[0]) ? "0" : parts[0];
                        number = new D512(intPart);
                        if (currentInput.StartsWith("-") && !number.IsZero()) number = -number;
                    }
                    else { number = new D512(currentInput); }
                }
                catch { }
                return;
            }

            isFloat = false;
            try { number = new D512(currentInput, GetCurrentBase()); } catch { }
        }

        private void SwitchMode(InputMode newMode)
        {
            if (currentMode == newMode) return;
            if (!string.IsNullOrEmpty(currentInput)) UpdateNumberFromInput();
            if (isFloat && newMode != InputMode.DEC) return;

            currentMode = newMode;
            if (!isFloat) currentInput = FormatNumberInCurrentMode(number);
            mainPanel.Invalidate();
        }

        private void ProcessOperation(Operation op)
        {
            if (op == Operation.NONE) return;
            if (IsOperationBlocked(op)) return;

            if (currentOp != Operation.NONE && string.IsNullOrEmpty(currentInput))
            {
                currentOp = op;
                mainPanel.Invalidate();
                return;
            }

            if (!string.IsNullOrEmpty(currentInput)) UpdateNumberFromInput();

            storedNumber = number;
            storedFloat = isFloat ? floatNumber : new F512(number.ToString());
            leftIsFloat = isFloat;
            currentOp = op;
            currentInput = "";

            hasResult = false;
            outputText = "";
            outputScrollOffset = 0;
            scrollToBottomInput = true;

            mainPanel.Invalidate();
        }

        private void ProcessEquals()
        {
            if (currentOp == Operation.NONE)
            {
                if (!string.IsNullOrEmpty(currentInput)) UpdateNumberFromInput();
                outputText = isFloat ? floatNumber.ToString() : FormatNumberInCurrentMode(number);
                hasResult = true;
                outputScrollOffset = 0;
                mainPanel.Invalidate();
                return;
            }

            if (!string.IsNullOrEmpty(currentInput)) UpdateNumberFromInput();

            Operation op = currentOp;
            bool useFloatMath = currentMode == InputMode.DEC && (leftIsFloat || isFloat);

            try
            {
                if (useFloatMath)
                {
                    if (!IsArithmeticOperation(op))
                    {
                        outputText = "Float supports only + - × ÷";
                        hasResult = true;
                        currentOp = Operation.NONE;
                        currentInput = "";
                        leftIsFloat = false;
                        mainPanel.Invalidate();
                        return;
                    }

                    F512 left = leftIsFloat ? storedFloat : new F512(storedNumber.ToString());
                    F512 right = isFloat ? floatNumber : new F512(number.ToString());
                    F512 result = new F512("0");

                    switch (op)
                    {
                        case Operation.ADD: result = left + right; break;
                        case Operation.SUB: result = left - right; break;
                        case Operation.MUL: result = left * right; break;
                        case Operation.DIV: result = left / right; break;
                    }

                    outputText = $"{left} {GetOperationSymbol(op)} {right} = {result}";
                    hasResult = true;
                    outputScrollOffset = 0;

                    floatNumber = result;
                    SyncIntegerPartFromFloat(result);

                    string rs = result.ToString();
                    isFloat = rs.Contains(".");
                    currentOp = Operation.NONE;
                    currentInput = "";
                    leftIsFloat = false;
                    mainPanel.Invalidate();
                    return;
                }

                D512 rightD = number;
                D512 resultD = new D512(0);

                switch (op)
                {
                    case Operation.ADD: resultD = storedNumber + rightD; break;
                    case Operation.SUB: resultD = storedNumber - rightD; break;
                    case Operation.MUL: resultD = storedNumber * rightD; break;
                    case Operation.DIV:
                        if (rightD.IsZero()) throw new DivideByZeroException();
                        resultD = storedNumber / rightD;
                        break;
                    case Operation.AND: resultD = storedNumber & rightD; break;
                    case Operation.OR: resultD = storedNumber | rightD; break;
                    case Operation.XOR: resultD = storedNumber ^ rightD; break;
                    case Operation.SHL: resultD = storedNumber << (int)((long)rightD & 0x1FF); break;
                    case Operation.SHR: resultD = storedNumber >> (int)((long)rightD & 0x1FF); break;
                    default: return;
                }

                string leftText = FormatNumberInCurrentMode(storedNumber);
                string rightText = FormatNumberInCurrentMode(rightD);
                string resultText = FormatNumberInCurrentMode(resultD);

                outputText = $"{leftText} {GetOperationSymbol(op)} {rightText} = {resultText}";
                hasResult = true;
                outputScrollOffset = 0;

                number = resultD;
                floatNumber = new F512(resultD.ToString());

                isFloat = false;
                leftIsFloat = false;
                currentOp = Operation.NONE;
                currentInput = FormatNumberInCurrentMode(number);
            }
            catch
            {
                outputText = "Error";
                hasResult = true;
                outputScrollOffset = 0;
                currentOp = Operation.NONE;
                currentInput = "";
                leftIsFloat = false;
            }

            mainPanel.Invalidate();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void ProcessNOT()
        {
            if (!string.IsNullOrEmpty(currentInput)) UpdateNumberFromInput();
            if (isFloat) { outputText = "NOT requires integer"; hasResult = true; mainPanel.Invalidate(); return; }

            number = ~number;
            floatNumber = new F512(number.ToString());
            outputText = $"~ = {FormatNumberInCurrentMode(number)}";
            hasResult = true;
            currentInput = FormatNumberInCurrentMode(number);
            mainPanel.Invalidate();
        }

        private void ProcessBackspace()
        {
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                isFloat = currentInput.Contains(".");
                scrollToBottomInput = true;
                UpdateNumberFromInput();
                hasResult = false;
                outputText = "";
                outputScrollOffset = 0;
                mainPanel.Invalidate();
            }
        }

        private void ProcessClear()
        {
            currentInput = "";
            number = new D512(0);
            floatNumber = new F512("0");
            storedFloat = new F512("0");
            isFloat = false;
            leftIsFloat = false;
            currentOp = Operation.NONE;
            storedNumber = new D512(0);
            outputText = "";
            hasResult = false;
            outputScrollOffset = 0;
            inputScrollOffset = 0;
            basesScrollOffset = 0;
            mainPanel.Invalidate();
        }

        private void ProcessDot()
        {
            if (currentMode != InputMode.DEC) return;
            BeginNewInputAfterResult();
            if (currentInput.Contains(".")) return;

            if (string.IsNullOrEmpty(currentInput)) currentInput = "0.";
            else currentInput += ".";

            isFloat = true;
            scrollToBottomInput = true;
            mainPanel.Invalidate();
        }

        private void ProcessNegate()
        {
            if (currentMode != InputMode.DEC) return;

            if (string.IsNullOrEmpty(currentInput))
            {
                if (isFloat)
                {
                    string fs = floatNumber.ToString();
                    fs = fs.StartsWith("-") ? fs.Substring(1) : "-" + fs;
                    floatNumber = new F512(fs);
                    SyncIntegerPartFromFloat(floatNumber);
                    outputText = $"Sign changed: {floatNumber}";
                }
                else
                {
                    number = -number;
                    floatNumber = new F512(number.ToString());
                    currentInput = FormatNumberInCurrentMode(number);
                    outputText = $"Sign changed: {currentInput}";
                }
                hasResult = true;
            }
            else
            {
                currentInput = currentInput.StartsWith("-") ? currentInput.Substring(1) : "-" + currentInput;
                UpdateNumberFromInput();
            }

            mainPanel.Invalidate();
        }

        private void CopyOutputToClipboard()
        {
            if (hasResult && !string.IsNullOrEmpty(outputText))
                Clipboard.SetText(outputText);
        }

        private void Panel_MouseClick(object sender, MouseEventArgs e)
        {
            if (copyBtn.Contains(e.Location)) { CopyOutputToClipboard(); return; }
            if (hexBtn.Contains(e.Location) && !isFloat) { SwitchMode(InputMode.HEX); return; }
            if (decBtn.Contains(e.Location)) { SwitchMode(InputMode.DEC); return; }
            if (octBtn.Contains(e.Location) && !isFloat) { SwitchMode(InputMode.OCT); return; }
            if (binBtn.Contains(e.Location) && !isFloat)
            {
                SwitchMode(currentMode == InputMode.BIN_GRID ? InputMode.BIN_KEYBOARD : InputMode.BIN_GRID);
                return;
            }

            if (addBtn.Contains(e.Location)) { ProcessOperation(Operation.ADD); return; }
            if (subBtn.Contains(e.Location)) { ProcessOperation(Operation.SUB); return; }
            if (mulBtn.Contains(e.Location)) { ProcessOperation(Operation.MUL); return; }
            if (divBtn.Contains(e.Location)) { ProcessOperation(Operation.DIV); return; }
            if (andBtn.Contains(e.Location)) { ProcessOperation(Operation.AND); return; }
            if (orBtn.Contains(e.Location)) { ProcessOperation(Operation.OR); return; }
            if (xorBtn.Contains(e.Location)) { ProcessOperation(Operation.XOR); return; }
            if (shlBtn.Contains(e.Location)) { ProcessOperation(Operation.SHL); return; }
            if (shrBtn.Contains(e.Location)) { ProcessOperation(Operation.SHR); return; }
            if (notBtn.Contains(e.Location)) { ProcessNOT(); return; }
            if (clearBtn.Contains(e.Location)) { ProcessClear(); return; }
            if (backspaceBtn.Contains(e.Location)) { ProcessBackspace(); return; }
            if (equalsBtn.Contains(e.Location)) { ProcessEquals(); return; }
            if (dotBtn.Contains(e.Location)) { ProcessDot(); return; }
            if (signBtn.Contains(e.Location)) { ProcessNegate(); return; }

            string[] keys = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F" };
            for (int i = 0; i < numButtons.Length; i++)
                if (numButtons[i].Contains(e.Location)) { ProcessInput(keys[i]); return; }

            if (currentMode == InputMode.BIN_GRID) ProcessBitGridClick(e.Location);
        }

        private void ProcessBitGridClick(Point clickPoint)
        {
            int cs = 16, gx = 2, gy = 4, gg = 8, sx = 20, sy = 450;
            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 32; c++)
                {
                    int idx = 511 - (r * 32 + c);
                    int x = sx + c * (cs + gx) + (c / 8) * gg;
                    int y = sy + r * (cs + gy);
                    if (new Rectangle(x, y, cs, cs).Contains(clickPoint))
                    {
                        bool bit = number.GetBit((uint)idx);
                        number.SetBit(bit ? 0UL : 1UL, (uint)idx);
                        currentInput = FormatNumberInCurrentMode(number);
                        isFloat = false;
                        floatNumber = new F512(number.ToString());
                        outputText = $"Bit {idx} = {(!bit ? 1 : 0)}";
                        hasResult = true;
                        mainPanel.Invalidate();
                        return;
                    }
                }
            }
        }

        private void Panel_MouseWheel(object sender, MouseEventArgs e)
        {
            void HandleScroll(VScrollBar sb, ref int offset)
            {
                if (sb.Visible)
                {
                    int step = e.Delta > 0 ? -sb.LargeChange : sb.LargeChange;
                    int nv = sb.Value + step;
                    nv = Math.Max(0, Math.Min(nv, sb.Maximum - sb.LargeChange + 1));
                    sb.Value = nv;
                    offset = nv;
                    mainPanel.Invalidate();
                }
            }

            if (outputField.Contains(e.Location)) HandleScroll(outputScrollBar, ref outputScrollOffset);
            else if (inputField.Contains(e.Location)) HandleScroll(inputScrollBar, ref inputScrollOffset);
            else if (basesField.Contains(e.Location)) HandleScroll(basesScrollBar, ref basesScrollOffset);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C) { CopyOutputToClipboard(); e.Handled = true; return; }
            if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) { ProcessInput((e.KeyCode - Keys.D0).ToString()); e.Handled = true; }
            else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) { ProcessInput((e.KeyCode - Keys.NumPad0).ToString()); e.Handled = true; }
            else if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.F) { ProcessInput(e.KeyCode.ToString()); e.Handled = true; }
            else if (e.KeyCode == Keys.Back) { ProcessBackspace(); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Escape) { ProcessClear(); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter) { ProcessEquals(); e.Handled = true; }
            else if (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.Decimal) { ProcessDot(); e.Handled = true; }
            else if (e.KeyCode == Keys.F9) { ProcessNegate(); e.Handled = true; }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Add: case Keys.Oemplus: ProcessOperation(Operation.ADD); e.Handled = true; break;
                    case Keys.Subtract: case Keys.OemMinus: ProcessOperation(Operation.SUB); e.Handled = true; break;
                    case Keys.Multiply: ProcessOperation(Operation.MUL); e.Handled = true; break;
                    case Keys.Divide: case Keys.OemQuestion: ProcessOperation(Operation.DIV); e.Handled = true; break;
                }
            }
        }

        private bool IsBitwiseOperation(Operation op)
        {
            return op == Operation.AND || op == Operation.OR || op == Operation.XOR || op == Operation.SHL || op == Operation.SHR;
        }

        private bool IsArithmeticOperation(Operation op)
        {
            return op == Operation.ADD || op == Operation.SUB || op == Operation.MUL || op == Operation.DIV;
        }

        private void BeginNewInputAfterResult()
        {
            if (hasResult && currentOp == Operation.NONE)
            {
                currentInput = "";
                outputText = "";
                hasResult = false;
                outputScrollOffset = 0;
                inputScrollOffset = 0;
                basesScrollOffset = 0;

                number = new D512(0);
                floatNumber = new F512("0");
                isFloat = false;
                leftIsFloat = false;
            }
        }

        private void SyncIntegerPartFromFloat(F512 value)
        {
            string s = value.ToString();
            string unsigned = s.TrimStart('-');
            string[] parts = unsigned.Split('.');
            string intPart = string.IsNullOrWhiteSpace(parts[0]) ? "0" : parts[0];
            number = new D512(intPart);
            if (s.StartsWith("-") && !number.IsZero()) number = -number;
        }
    }
}
