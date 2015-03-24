using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using mySockets;

namespace Ping_pong
{
    public partial class Form1 : Form
    {
        int counter_1 = 0; //счёт (читай, кол-во забитых квадратов) для первой ракетки
        int counter_2 = 0; //счёт для второй ракетки      

        Random rnd = new Random(); //генератор типаслучайных чисел
        trigonometry physX = new trigonometry(); //жалкая реализация физики/тригонометрии/еще какой-то хуеты

        List<bonus> bonuses = new List<bonus>();

        Timer timer = new Timer(); //счётчег для обработки движения квадрата-мяча
        Timer bonusTimer = new Timer(); //счётчег для бонусов
        Timer sinTimer = new Timer(); //счётчег для бонуса с синусом
        Timer bonusCloseTimer = new Timer(); //счётчиг для сброса бонусов

        Rendering rendering;

        mp mp = null;

        ball ball = null;

        racket racket_1 = null;
        racket racket_2 = null;

        //структура для сброса действия бонусов
        public struct BonusCooldown
        {
            public int sinSeconds;
            public int invisSeconds;
        }

        BonusCooldown cooldown;

        // { что-то непостижимо непонятное
        public Form1()
        {
            InitializeComponent();

            rendering = new Rendering(this);


            this.Width = Settings.Window.width;
            this.Height = Settings.Window.height;

            ball = new ball();


            score.Left = Convert.ToInt32((this.Width - score.Width) / 2); 
            score.Top = ClientSize.Height - score.Height - 10;

            racket_1 = new racket(this, true);
            racket_2 = new racket(this, false);

            switch (dataTransfer.gameType)
            {
                case 0: //vs AI
                    if (dataTransfer.aiDifficulty == 11)
                    {
                        score.Text = "0";
                        score.Left = Convert.ToInt32((this.Width - score.Width) / 2);
                    }

                    new AI(this, dataTransfer.aiDifficulty, racket_1, ball);
                    new playerRacket(this,racket_2);
                    break;

                case 1: //hotseat
                    new playerRacket(this, racket_1);
                    new playerRacket(this, racket_2);
                    break;

                case 2: //multiplayer
                    if (dataTransfer.typeOfNet == "server")
                    {
                        mp = new mpServer(this, racket_2, racket_1, ball, score);
                        new playerRacket(this, racket_1);
                    }
                    else
                    {
                        mp = new mpClient(this, racket_1, racket_2, ball, score);
                        new playerRacket(this, racket_2);
                    }   
                    break;

                case 3: //screensaver
                    new AI(this, 9, racket_1, ball);
                    new AI(this, 9, racket_2, ball);
                    break;
            }

            // инициализураыфвфыпкуопгзем основной таймер, отвечающий за передвижение квадрата-мяча
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = 10;
            timer.Start();

            //
            if (dataTransfer.typeOfNet != "client")
            {
                bonusTimer.Tick += new EventHandler(bonusTimer_Tick);
                bonusTimer.Interval = 1000;
                bonusTimer.Start();

                sinTimer.Tick += new EventHandler(sinTimer_Tick);
                sinTimer.Interval = 100;
            }

            bonusCloseTimer.Tick += new EventHandler(bonusCloseTimer_Tick);
            bonusCloseTimer.Interval = 1000;
            bonusCloseTimer.Start();


            // настраиваем физику
            physX.currPower = 5; // скорость (она же — сила пинка по мячику) передвижения
            if (rnd.Next(2) == 0) // начальный угол перемещения мячика в градусах (0 — вправо, 90 — вверх и т.д.)
                physX.deltaAngle = -60 + rnd.Next(120);
            else
                physX.deltaAngle = 120 + rnd.Next(120);

            physX.getProjection();   //фигачим проекцию на оси Хэ и Угрик.
            //в данной программе:
            //проекция — кол-во пикселей, на которое переместится мяч за один ход/тик/иличтотамещёунас
        }
        

        // { основной цикл, отвечающий за перемещение мяча и события
        public void timer_Tick(object sender, EventArgs e)
        {

            //обрабатываем получение бонуса
            bonus obj = HitTest(); //получаем бонус, которого коснулся мяч

            if (obj != null) //!!! — защита от чего-то неведомого
            {

                obj.Hide(); //прячем бонус...

                bonuses.Remove(obj); //и удаляем ссылку на него из общего списка бонусов

                switch (obj.getType()) //применяем бонус
                {
                    case 0: //увеличение ракетки
                        if ((physX.deltaX < 0) && (racket_2.Height < 250)) 
                            { 
                                racket_2.Height += 50;
                                if (racket_2.Bottom > ClientSize.Height - 10) { racket_2.Top = ClientSize.Height - 10 - racket_2.Height; } //чтобы ракетка не выходила за края поля
                            }

                        if ((physX.deltaX > 0) && (racket_1.Height < 250)) 
                            {
                                racket_1.Height += 50;
                                if (racket_1.Bottom > ClientSize.Height - 10) { racket_1.Top = ClientSize.Height - 10 - racket_1.Height; }
                            }
                        break;

                    case 1: //уменьшение ракетки
                        if ((physX.deltaX < 0) && (racket_2.Height > 50)) { racket_2.Height -= 50; }
                        if ((physX.deltaX > 0) && (racket_1.Height > 50) && (dataTransfer.aiDifficulty != 11)) { racket_1.Height -= 50; } // ...и если бот не имба
                        break;

                    case 2: //ускорение
                        physX.currPower += 10;
                        physX.getProjection();
                        break;

                    case 3: //замедление
                        physX.currPower = 3; //fixed by magic; do not touch you crazy motherfucker
                        physX.getProjection();
                        break;
                
                    case 4: //движение по синусоиде
                        sinTimer.Start();
                        physX.sinX = 0;
                        cooldown.sinSeconds = 4;
                        break;

                    case 5: //невидимый мячик
                        ball.Hide();
                        cooldown.invisSeconds = 2;
                        break; 
                }

                obj.Delete();
            }


            //рассчитываем траекторию движения мяча
            if (dataTransfer.typeOfNet != "client")
            {
                float wWidth = ClientSize.Width;
                float wHeight = ClientSize.Height;

                if (ball.Left <= 0) // если забили левому
                {
                    counter_2++; //счётчег++ второму 

                    ball.Location = new PointF(wWidth / 2 - ball.Width / 2, wHeight / 2 - ball.Height / 2); //выставляем мяч в начальную позицию
                    ResetBonuses(); //сбрасываем действие бонусов (сами бонусы никуда не исчезают)

                    physX.currPower = 5; // И ХЕРАЧИМ РАНДОООММ (см. описание в «типо автозапуске»)
                    physX.deltaAngle = rnd.Next(120) + 120;
                    physX.getProjection();

                    dataTransfer.goal = true; //временная переменная для мультиплеера


                    score.Text = counter_1 + " : " + counter_2; //вывод обновлённого счёта

                    score.Left = Convert.ToInt32((this.Width - score.Width) / 2); //выравнивание табло по центру
                }

                if (ball.Right >= wWidth) // если забили правому
                {
                    counter_1++; // и по аналогии

                    ball.Location = new PointF(wWidth / 2 - ball.Width / 2, wHeight / 2 - ball.Height / 2);
                    ResetBonuses();

                    physX.currPower = 5; // И ХЕРАЧИМ РАНДОООММ (см. описание в «типо автозапуске»)
                    physX.deltaAngle = rnd.Next(120) - 60;
                    physX.getProjection();

                    dataTransfer.goal = true; //временная переменная для мультиплеера

                    if (dataTransfer.aiDifficulty <= 10)
                        score.Text = counter_1 + " : " + counter_2; //вывод обновлённого счёта
                    else
                        score.Text = counter_1.ToString();

                    score.Left = Convert.ToInt32((wWidth - score.Width) / 2); //выравнивание табло по центру
                }


                if ((ball.Top >= wHeight - 25) || (ball.Top <= 5)) // если мячик ударяется о верхнюю или нижнюю грань
                {
                    physX.deltaAngle = -physX.deltaAngle; //расчитываем новый угол

                    if (ball.Top > wHeight - 25) { ball.Top = wHeight - 25; } else if (ball.Top < 5) { ball.Top = 5; }

                    if (physX.currPower <= 15) // ограничиваем максимальную скорость мяча
                        physX.currPower += 1; // прибавляем к текущей скорости +1

                    physX.getProjection();
                }

                if (ball.Left <= racket_1.Right + 5) //для левой ракетки
                {
                    if ((ball.Top + 25 > racket_1.Top) && (ball.Top < racket_1.Bottom)) //основная часть ракетки
                        physX.deltaAngle = 45 - 110 * (ball.Top - racket_1.Top) / racket_1.Height;


                    if ((ball.Top > racket_1.Top) && (ball.Top < racket_1.Bottom) && (physX.currPower <= 15))
                        physX.currPower += 1;

                    physX.getProjection();
                }

                if (ball.Right >= racket_2.Left - 5) //для правой ракетки
                {
                    if ((ball.Top + 25 > racket_2.Top) && (ball.Top < racket_2.Bottom)) //основная часть ракетки
                        physX.deltaAngle = 135 + 110*(ball.Top - racket_2.Top)/racket_2.Height;

                    if ((ball.Top > racket_2.Top) && (ball.Top < racket_2.Bottom) && (physX.currPower <= 15))
                        physX.currPower += 1;

                    physX.getProjection();
                }

                //собсно, перемещаем мяч
                ball.Left += physX.deltaX;
                ball.Top -= physX.deltaY + Convert.ToInt32(Math.Sin(physX.sinX)) * 5;

                
            }
            rendering.render();
        }
        // }


        bonus HitTest()
        {
            /*
             * Проверяем, коснулся ли мяч бонуса
             * Если да, то функция возвращает ссылку на этот бонус
             * Нет — ссылку на сраное ничего
            */

            foreach (bonus key in bonuses) //проверяем каждый бонус на предмет столкновения с мячом
            {
                float height = key.Height, width = key.Width;
                if ((ball.Bottom >= key.Top) & (ball.Top <= key.Top + height) & ((ball.Left <= key.Left + width) & (ball.Right >= key.Left)) & key.Visible)
                    return key;
            }

            return null;
        }

        public void ResetBonuses()
        {
            //движение по синусоиде
            sinTimer.Stop();
            physX.sinX = 0;

            //инвиз
            ball.Show();

            //сбрасываем размеры палок
            if (dataTransfer.aiDifficulty != 11) // если бот не имба
                racket_1.Height = 100;

            racket_2.Height = 100;
        }

        void bonusTimer_Tick (object sender, EventArgs e) //создание бонуса
        {
            createBonus(new Point(rnd.Next(50, ClientSize.Width - 140),rnd.Next(50,ClientSize.Height - 140)),rnd.Next(6) == 5);

            if (dataTransfer.net != null)
                dataTransfer.lastBonus = bonuses[bonuses.Count - 1];

            bonusTimer.Interval = rnd.Next(Settings.Gameplay.minBonusesSpawnTime, Settings.Gameplay.maxBonusesSpawnTime);
        }

        public void createBonus (Point location, bool random = false, int type = -1)
        {
            /*
             * Создаём бонус. Поддерживаем максимальное число бонусов равным параметру Settings.Gameplay.maxBonusesCount.
             * Если бонусов больше, чем нужно, то удаляем первый (самый старый) бонус
             * Добавляем бонус в список с бонусами
            */

            if (bonuses.Count > Settings.Gameplay.maxBonusesCount - 1)
            {
                bonuses[0].Hide();
                bonuses[0].Dispose();
                bonuses.Remove(bonuses[0]);
            }

            bonuses.Add(new bonus(this, location, random, type));
        }

        
        void sinTimer_Tick (object sender, EventArgs e)
        {
            //бонус для движения мяча по синусоиде (увеличивает аргумент для функции Math.Sin)

            physX.sinX += 2;            
        }

        void bonusCloseTimer_Tick (object sender, EventArgs e) //сброс бонусов
        {
            //колбэк для отключения эффуктов от бонусов. укорачивает жизнь эффекта каждую секунду

            if (cooldown.sinSeconds > 0) { cooldown.sinSeconds--; }
            else { sinTimer.Stop(); physX.sinX = 0; }

            if (cooldown.invisSeconds > 0) { cooldown.invisSeconds--; }
            else { ball.Show(); }
        }

        private void Form1_FormClosed (object sender, FormClosedEventArgs e)
        {
            /*
             * Если запущена сетевая игра, то останавливаем все компоненты, связанные с игрой по сети 
             * TODO: очищаем все возможные данные, связанные с мультиком
             * И отсылаем сигнал о том, что мы выключаемся, оппоненту, конечно же
             * 
             * Ну и общее для всех типов игры открытие меню
            */

            if (dataTransfer.typeOfNet == "server" && dataTransfer.net != null) //если мы выходим первыми (определяем по необнулённому объекту net)
            {
                mp.stopReceive(); //прекращаем принимать данные
                dataTransfer.net.serverSend("q"); //посылаем сигнал о том, что мы выходим

                while (dataTransfer.net.serverReceive() != "q") //ждём ответного q(uitted)
                {
                    ;
                }

                dataTransfer.net.close(); //закрываем соединение
            }
            
            if (dataTransfer.typeOfNet == "client" && dataTransfer.net != null) //то же самое, но для клиента
            {
                mp.stopReceive();
                dataTransfer.net.clientSend("q");

                while (dataTransfer.net.clientReceive() != "q")
                {
                    ;
                }

                dataTransfer.net.close();
            }


            bonuses.Clear();
            timer.Stop();
            bonusTimer.Stop();
            bonusCloseTimer.Stop();

            Program.myMenu.Show(); //показываем меню
        }

        public float getDeltaX()
        {
            return physX.deltaX;
        }

        public void setDeltaX(float DeltaX)
        {
            physX.deltaX = DeltaX;
        }
    }

    
    public static class Settings
    {
        public class Rackets
        {
            public static int speed = 10; //на какое расстояние (в пикселях) сдвинется ракетка
            public static Keys fUp = Keys.W; //какая кнопка отвечает за движение первой (левой) ракетки вверх
            public static Keys fDown = Keys.S; //...вниз

            public static Keys sUp = Keys.Up; //какая кнопка отвечает за движение первой (левой) ракетки вверх
            public static Keys sDown = Keys.Down; //...вниз
        }

        public class Window
        {
            public static int width = 600;
            public static int height = 600;
        }

        public class Gameplay
        {
            public static int maxBonusesCount = 5;
            public static int minBonusesSpawnTime = 3000;
            public static int maxBonusesSpawnTime = 10000;

            public static int scoreLimit = 21;
        }
    }


    //класс для вычислений перемещения мяча
    //TODO: запихать эту херь в класс ball
    public class trigonometry  
    {
        public float deltaX; //передвижение мяча за один тик по координате Икс (в пикселях)
        public float deltaY; //по координате Угрик
        public float deltaAngle; //угол передвижения мяча (0 градусов — право; 90 градусов — верх и тэдэ)
                               //из угла вычисляем deltaX и deltaY
        public float currPower; //импровизируемая сила, от которой зависит скорость мяча
        public float sinX = 0; //переменная, необходимая для бонуса «движение по синусоиде»
                                //является аргументом в функции Math.sin

        float DegreeToRadian(float angle) //переводим градусы в радианы
        {
            return (float)Math.PI * angle / 180;
        }

        public void getProjection()
        {
            double angle = DegreeToRadian(deltaAngle);
            
            deltaX = currPower * (float)Math.Cos(angle);
            deltaY = currPower * (float)Math.Sin(angle);
        }
    }


    

    //класс для бота
    public class AI 
    {
        string name = "automaton";

        int difficulty; //сложность. от неё зависит, как быстро может передвигаться ракетка бота, и, как далеко она может видеть
        Form owner;

        racket racket = null;
        ball ball = null;

        /// <summary>
        /// Конструктор для бота
        /// </summary>
        /// <param name="difficulty">Определяет, как быстро будет перемещаться ракетка бота, и, как далеко она видит. Может принимать значения от 1 до 11 (11 — имбобот).</param>
        /// <param name="controlledRacket">Какую ракетку будет контролировать бот</param>
        /// <param name="ball">Ссылка на объект-мяч</param>
        public AI(Form owner, int difficulty, racket controlledRacket, ball ball)
        {
            //копируем аргументы в локальные переменные
            this.difficulty = difficulty;
            racket = controlledRacket;
            this.owner = owner;
            this.ball = ball;

            if (difficulty <= 10)
            {
                //объявляем таймер, который будет управлять ракеткой бота, и запускаем его
                Timer aiTimer = new Timer();
                if (difficulty != 0) aiTimer.Interval = 10;
                aiTimer.Tick += new EventHandler(aiTimer_Tick);
                aiTimer.Start();
            }
            else
            {
                racket.Height = owner.ClientSize.Height - 24;
                racket.Top = 12;
            }
        }

        void aiTimer_Tick(object sender, EventArgs e)
        {
            float rTop = racket.Top;
            float rBottom = racket.Bottom;
            float rHeight = racket.Height;

            float bTop = ball.Top;
            float bBottom = ball.Bottom;

            if (ball.Left < owner.ClientSize.Width / 1.5 + 30 * difficulty) //определяем, на каком расстоянии бот начинает реагировать на мяч
            {
                if (rTop > 10 && rBottom < owner.ClientSize.Height) //ограничиваем перемещение бота размерами поля
                {
                    if (bTop > rBottom - rHeight / 2) //если мяч ниже
                        racket.Top += difficulty;
                    else if (bBottom < rTop + rHeight / 2) //если мяч выше
                        racket.Top -= difficulty;
                }
            }

            if (racket.Top <= 10) // костыль (чтобы ракетка не выходила за края поля)
                racket.Top = 11;
            if (racket.Bottom >= owner.ClientSize.Height - 20)
                racket.Top = owner.ClientSize.Height - 20 - rHeight;

            //«GOD MODE» is off
            //Program.myForm.racket_1.Top = Program.myForm.ball.Top + 10 - Program.myForm.racket_1.Height / 2;  
            //Program.myForm.racket_2.Top = Program.myForm.ball.Top + 10 - Program.myForm.racket_1.Height / 2;
        }
    }


    public class Rendering
    {
        static LinkedList<GameObject> objects = new LinkedList<GameObject>();

        BufferedGraphicsContext bufferContext = BufferedGraphicsManager.Current;
        BufferedGraphics canvas = null;
        Form1 form = null;

        public Rendering(Form1 form)
        {
            this.form = form;
            canvas = bufferContext.Allocate(form.CreateGraphics(), form.DisplayRectangle);
        }


        public void render()
        {
            canvas.Graphics.Clear(Color.FromArgb(25,25,25));

            string text = String.Empty;
            string text2 = String.Empty;

            foreach (GameObject obj in objects)
            {
                drawObject(obj);
                text = text + obj.Top.ToString() + "; ";
                text2 = text2 + obj.Left.ToString() + "; ";
            }

            //for debugging purposes

            canvas.Graphics.DrawString(text, new Font(FontFamily.GenericMonospace, 9), new SolidBrush(Color.WhiteSmoke), new PointF(20, 20));
            canvas.Graphics.DrawString(text2, new Font(FontFamily.GenericMonospace, 9), new SolidBrush(Color.WhiteSmoke), new PointF(20, 40));

            canvas.Render();
        }


        void drawObject(GameObject obj)
        {
            if (obj.Visible)
            {
                if (obj.Image == null)
                    canvas.Graphics.FillRectangle(new SolidBrush(obj.BackColor), new RectangleF(obj.Location, obj.Size));
                else
                    canvas.Graphics.DrawImage(obj.Image, obj.Location);
            }
        }

        public class GameObject
        {
            
            public GameObject()
            {
                objects.AddFirst(this);
            }

            float left = 0;
            public float Left {
                get { return left; }
                set { left = value; right = value + width; location.X = value; }
                }

            float right = 0;
            public float Right
            {
                get { return right; }
                set { right = value; left = value - width; location.X = left; }
            }

            float top = 0;
            public float Top
            {
                get { return top; }
                set { top = value; bottom = value + height; location.Y = value; }
            }

            float bottom = 0;
            public float Bottom
            {
                get { return bottom; }
                set { bottom = value; top = value - height; location.Y = top; }
            }


            float height = 0;
            public float Height {
                get { return height; }
                set { height = value; bottom = top + value; size.Height = value; }
            }
            
            float width = 0;
            public float Width {
                get { return width; }
                set { width = value; right = left + value; size.Width = value; }
            }

            public Color BackColor;
            public string Name = "PronStar";

            public bool Visible = true;

            public System.Drawing.Bitmap Image;
            public PointF Location {
                get { return location; }
                set { location = value; Left = value.X; Top = value.Y; }
            }

            PointF location = new PointF(0,0);


            public System.Drawing.SizeF Size {
                get { return size; }
                set { size = value; Width = value.Width; Height = value.Height; }
            }
           
            System.Drawing.SizeF size = new SizeF(0, 0);


            public GameObject Copy(GameObject obj)
            {
                //TODO
                return obj;
            }

            public void Delete()
            {
                objects.Remove(objects.Find(this));
            }

            public void Hide()
            {
                Visible = false;
            }

            public void Dispose()
            {
            }

            public void Show()
            {
                Visible = true;
            }
        }
    }


    //класс для бонусов
    public class bonus : Rendering.GameObject
    {
        Random rnd = new Random(); //...

        /*
         *свойство объекта, хранящее данные о виде бонуса
         *если равно 0 — увеличение ракетки
         *1 — уменьшение ракетки
         *2 — ускорение мяча
         *3 — замедление мяча
         *4 — движение мяча по синусоиде
         *5 — невидимый мяч
        */
        int typeOf;

        //определяем, наклыдавается ли на бонус картинка с вопросительным знаком (случайный бонус)
        bool random;

        public bonus(Form owner, Point location, bool Random = false, int TypeOf = -1)
        {

            this.Location = location;

            //если нужно создать рандомный бонус, создаём его
            //иначе, присваиваем какой-то конкретный вид бонуса, указанный в аргументе TypeOf

            if (TypeOf == -1)
                typeOf = rnd.Next(6);
            else
                typeOf = TypeOf;

            this.Height = 50;
            this.Width = 50;

            random = Random;

            if (Random) //случайный ли бонус
            {
                this.Image = Ping_pong.Properties.Resources._random;
            }
            else
            {
                switch (typeOf) //присваиваем картинку для каждого из видов бонуса
                {
                    case 0:
                        this.Image = Ping_pong.Properties.Resources._00;
                        break;
                    case 1:
                        this.Image = Ping_pong.Properties.Resources._01;
                        break;
                    case 2:
                        this.Image = Ping_pong.Properties.Resources._02;
                        break;
                    case 3:
                        this.Image = Ping_pong.Properties.Resources._03;
                        break;
                    case 4:
                        this.Image = Ping_pong.Properties.Resources._04;
                        break;
                    case 5:
                        this.Image = Ping_pong.Properties.Resources._05;
                        break;
                }
            }
            
            //добавляем на форму
            //owner.Controls.Add(this);
        }

        public int getType()
        {
            return typeOf;
        }

        public bool isRandom()
        {
            return random;
        }
    }

    public class ball : Rendering.GameObject
    {
        public ball()
        {
            this.Height = 20;
            this.Width = 20;

            this.Left = (this.Width - this.Width) / 2;
            this.Top = (this.Height - this.Height) / 2;
            this.BackColor = Color.White;
        }
    }

    public class racket : Rendering.GameObject //класс для ракеток
    {

        bool left;

        public bool getOrientation()
        {
            return left;
        }

        public racket(Form owner, bool Left)
        {
            this.BackColor = System.Drawing.Color.White;          
            this.Name = "racket";        
            this.Size = new System.Drawing.Size(20, 100);
            //this.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            //this.TabIndex = 1;
            //this.TabStop = false;

            left = Left;

            int wHeight = owner.ClientSize.Height;
            int wWidth = owner.ClientSize.Width;

            if (Left)
                this.Location = new System.Drawing.Point(15, wHeight/2 - 24 - 50);
            else
                this.Location = new System.Drawing.Point(wWidth - 20 - 12, wHeight / 2 - 24 - 50);

            //owner.Controls.Add(this);
        }
   
    }



    class playerRacket // Управление ракетками, йоу
    {
        Timer tmrRacketUp = new Timer();
        Timer tmrRacketDown = new Timer();

        racket racket = null;
       
        bool left;
        int speed;
        Form owner;
        Keys fUp, fDown, sUp, sDown;

        public playerRacket(Form owner, racket Racket)
        {

            left = Racket.getOrientation();
            speed = Settings.Rackets.speed;
            fUp = Settings.Rackets.fUp;
            fDown = Settings.Rackets.fDown;
            sUp = Settings.Rackets.sUp;
            sDown = Settings.Rackets.sDown;

            this.owner = owner;
            racket = Racket;
            
            tmrRacketUp.Interval = 10;
            tmrRacketUp.Tick += new System.EventHandler(tmrRacketUp_Tick);

            tmrRacketDown.Interval = 10;
            tmrRacketDown.Tick += new System.EventHandler(tmrRacketDown_Tick);

            owner.KeyDown += new System.Windows.Forms.KeyEventHandler(Form1_KeyDown);
            owner.KeyUp += new System.Windows.Forms.KeyEventHandler(Form1_KeyUp);

        }

        void Form1_KeyDown(object sender, KeyEventArgs e)  //Чувак, это нажим
        {
            Keys key = e.KeyCode;
            int wHeight = owner.ClientSize.Height;

            //палка адын
            if (left)
            {
                if ((key == fUp) && (racket.Top > 10))
                    tmrRacketUp.Start();
                else if ((key == fDown) && racket.Bottom < wHeight - 10)
                    tmrRacketDown.Start();
            }

            //палка два
            if (!left)
            {
                if ((key == sUp) && (racket.Top > 10))
                    tmrRacketUp.Start();
                else if ((key == sDown) && (racket.Bottom < wHeight - 10))
                    tmrRacketDown.Start();
            }
                
        }
        
        void Form1_KeyUp(object sender, KeyEventArgs e)  //Братюнь, это отжим
        {
            Keys key = e.KeyCode;

            //палка адын
            if (left)
            {
                if (key == fUp)
                    tmrRacketUp.Stop();
                else if (key == fDown)
                    tmrRacketDown.Stop();
            }

            //палка два

            if (!left)
            {
                if (key == sUp)
                    tmrRacketUp.Stop();
                else if (!left && key == sDown)
                    tmrRacketDown.Stop();
            }

        }

        void tmrRacketUp_Tick(object sender, EventArgs e) //таймер; плавно едем ПЕРВОЙ ракеткой ВВЕРХ (если есть нажим)
        {
            if (racket.Top > 10)
            {
                if (racket.Top - speed < 10)
                    racket.Top = 10;
                else
                    racket.Top -= speed;
            }
        }

        void tmrRacketDown_Tick(object sender, EventArgs e) //таймер; плавно едем ВТОРОЙ ракеткой ВНИЗ, йопт (если есть нажим)
        {
            int wHeight = owner.ClientSize.Height;
            if (racket.Bottom < wHeight - 10)
            {
                if (racket.Bottom + speed > wHeight - 10)
                    racket.Top = wHeight - 10 - racket.Height;
                else
                    racket.Top += speed;
            }
        }
    }



    class mp
    {
        protected Timer receiveTimer = new Timer();

        protected racket racket = null;

        public void stopReceive()
        {
            receiveTimer.Stop();
        }

    }



    class mpServer : mp //класс для мультика(сервер)
    {
        ball ball;

        Label score;

        racket yourRacket;

        Form1 owner;

        public mpServer(Form1 Owner, racket Racket, racket YourRacket, ball Ball, Label Score)
        {
            racket = Racket;
            ball = Ball;
            score = Score;
            yourRacket = YourRacket;

            owner = Owner;

            receiveTimer.Interval = 10;
            receiveTimer.Start();
            receiveTimer.Tick += new System.EventHandler(receive_Tick);

            sendSnapshot();
        }

        void sendSnapshot()
        {
            string message = String.Empty;

            message += "b!" + ball.Left + "," + ball.Top + "," + owner.getDeltaX() + ";";
            message += "r!" + yourRacket.Top + ";";           

            if (dataTransfer.lastBonus != null)
            {
                message += "p!" + dataTransfer.lastBonus.Left + "," + dataTransfer.lastBonus.Top + "," + dataTransfer.lastBonus.getType() + "," + (dataTransfer.lastBonus.isRandom() ? "1" : "0") + ";";
                dataTransfer.lastBonus = null;
            }

            if (dataTransfer.goal)//если кто-то соснул и ему забили
            {
                message += "s!" + score.Text + ";";
                dataTransfer.goal = false;
            }

            dataTransfer.net.serverSend(message);
        }

        void receive_Tick(object sender, EventArgs e)
        {
            string answer = dataTransfer.net.serverReceive();


            if (answer == "q")
            {
                stopReceive();
                dataTransfer.net.serverSend("q");
                dataTransfer.net.close();
                dataTransfer.net = null;
                MessageBox.Show("Оппонент отключился\r\nСчёт — " + score.Text);
                owner.Close();
            }
            else if (answer != "") 
            {
                racket.Top = Convert.ToInt32(answer.Split('!')[1]);
                sendSnapshot();
            }
        }
    }



    class mpClient : mp //класс для мультика(клиент)
    {
        ball ball;

        Form1 owner;

        Label score;

        racket yourRacket;


        public mpClient(Form1 Owner, racket Racket, racket YourRacket, ball Ball, Label Score)
        {
            ball = Ball;
            owner = Owner;
            racket = Racket;
            score = Score;
            yourRacket = YourRacket;

            receiveTimer.Interval = 1;
            receiveTimer.Start();
            receiveTimer.Tick += new System.EventHandler(receive_Tick);
        }

        void sendSnapshot()
        {
            dataTransfer.net.clientSend("r!" + yourRacket.Top);
        }

        void receive_Tick(object sender, EventArgs e)
        {
            string answer = dataTransfer.net.clientReceive();

            if (answer == "q")
            {
                stopReceive();
                dataTransfer.net.clientSend("q");
                dataTransfer.net.close();
                dataTransfer.net = null;
                MessageBox.Show("Оппонент отключился\r\nСчёт — " + score.Text);
                owner.Close();
            }
            else if (answer != "")
            {
                string[] splitted = answer.Split(';');

                foreach (string key in splitted)
                {
                    if (key != "")
                    {
                        string temp = String.Empty;

                        temp = key.Split('!')[1]; //часть строки со значениями

                        int x, y;

                        if (temp != "")
                            switch (key[0])
                            {
                                case 'b':
                                    x = Convert.ToInt32(temp.Split(',')[0]);
                                    y = Convert.ToInt32(temp.Split(',')[1]);
                                    int deltaX = Convert.ToInt32(temp.Split(',')[2]);

                                    owner.setDeltaX(deltaX);
                                    ball.Location = new Point(x, y);
                                    break;

                                case 'r':
                                    racket.Top = Convert.ToInt32(temp);
                                    break;

                                case 'p':
                                    x = Convert.ToInt32(temp.Split(',')[0]);
                                    y = Convert.ToInt32(temp.Split(',')[1]);
                                    int type = Convert.ToInt32(temp.Split(',')[2]);

                                    bool random = temp.Split(',')[3] == "1" ? true : false;

                                    owner.createBonus(new Point(x, y), random, type); 
                                    break;

                                case 's':
                                    score.Text = temp;
                                    owner.ResetBonuses();                                 
                                    break;
                            }
                    }
                }
             
                sendSnapshot();
            }
        }
    }
}