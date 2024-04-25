using System;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

namespace SSnakeGUI
{
    public partial class Form1 : Form
    {
        private PictureBox[,] ground;
        private bool[,] ground_info;
        private int head_x, head_y, tail_x, tail_y, food_x, food_y;
        private int w, h, score;

        private ResourceManager Resources = new ResourceManager("SSnakeGUI.Resources", Assembly.GetExecutingAssembly());

        private enum Directions { stop, up, right, down, left };
        private Directions head_dir;

        private QueueNode<Directions> movement;
        private QueueNode<Directions> tail_move;
        private Directions current_direction;
        private bool on_pause;

        private Random food_location_gen;

        private Timer timer;
        private Label l;

        public Form1()
        {
            InitializeComponent();
            Load += new System.EventHandler(this.Form1_Load);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            w = 30;
            h = 15;
            score = 0;
            ground = new PictureBox[h, w];
            ground_info = new bool[h, w];
            int sx = 20, sy = 20;
            int st_size = 30;

            for (int i = 0; i < h; ++i)
            {
                for (int j = 0; j < w; ++j)
                {
                    ground[i, j] = new PictureBox
                    {
                        Size = new Size(st_size, st_size),
                        Location = new Point(sx + j * st_size, sy + i * st_size),
                        Image = (Bitmap)Resources.GetObject("none"),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Enabled = false
                    };
                    ground_info[i, j] = false;
                    Controls.Add(ground[i, j]);
                }
            }
            Size = new Size(2 * w + (w + 1) * st_size, 2 * h + (h + 2) * st_size);

            tail_x = w / 6; head_x = tail_x + 4;
            tail_y = head_y = h / 2;

            //MessageBox.Show(string.Format("tail: ({0}, {1}), head: ({2}, {3})", tail_x, tail_y, head_x, head_y));

            for (int i = tail_x + 1; i < head_x; ++i)
            {
                ground[head_y, i].Image = (Bitmap)Resources.GetObject("body_horizontal");
                ground_info[head_y, i] = true;
            }

            ground[head_y, head_x].Image = (Bitmap)Resources.GetObject("head__right");
            ground_info[head_y, head_x] = true;
            ground[tail_y, tail_x].Image = (Bitmap)Resources.GetObject("tail__left");
            ground_info[tail_y, tail_x] = true;

            food_location_gen = new Random();
            while (true)
            {
                food_x = food_location_gen.Next(0, w);
                food_y = food_location_gen.Next(0, h);
                if (ground_info[food_y, food_x] == false)
                {
                    int food_form = food_location_gen.Next(0, 3);
                    ground[food_y, food_x].Image = (food_form == 0) ? (Bitmap)Resources.GetObject("food_banana") : ((food_form == 1) ? (Bitmap)Resources.GetObject("food_orange") : (Bitmap)Resources.GetObject("food_apple"));
                    break;
                }
            }

            timer = new Timer
            {
                Interval = 250,
                Enabled = false
            };
            timer.Tick += new EventHandler(Move_body);

            KeyDown += new KeyEventHandler(Control);

            on_pause = false;

            movement = new QueueNode<Directions>();
            tail_move = new QueueNode<Directions>();
            tail_move.Add(Directions.left);
            tail_move.Add(Directions.left);
            tail_move.Add(Directions.left);
            tail_move.Add(Directions.left);

            current_direction = Directions.stop;
            head_dir = Directions.right;

            l = new Label
            {
                Location = new Point(ground[h - 1, 0].Location.X, ground[h - 1, 0].Location.Y + ground[0, 0].Height),
                Text = "A",
                Size = new Size(w * ground[0, 0].Width, (h - 5) * ground[h - 1, 0].Height),
                BackColor = Color.Green,
                Font = new Font("Arial", 12f)
            };
            Controls.Add(l);

            timer.Enabled = true;
        }

        void Control(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Subtract)
                timer.Interval += 50;
            else if (e.KeyData == Keys.Add && timer.Interval > 100)
                timer.Interval -= 50;
            else if (e.KeyData == Keys.P)
            {
                if (on_pause)
                {
                    on_pause = false;
                    timer.Enabled = true;
                }
                else
                {
                    on_pause = true;
                    timer.Enabled = false;
                }
            }
            if (!on_pause)
            {
                if (e.KeyData == Keys.W || e.KeyData == Keys.Up && !(movement.Last == Directions.up || movement.Last == Directions.down))
                { movement.Add(Directions.up); }
                if (e.KeyData == Keys.A || e.KeyData == Keys.Left && !(movement.Last == Directions.left || movement.Last == Directions.right))
                { movement.Add(Directions.left); }
                if (e.KeyData == Keys.S || e.KeyData == Keys.Down && !(movement.Last == Directions.up || movement.Last == Directions.down))
                { movement.Add(Directions.down); }
                if (e.KeyData == Keys.D || e.KeyData == Keys.Right && !(movement.Last == Directions.left || movement.Last == Directions.right))
                { movement.Add(Directions.right); }
            }
        }

        void Move_body(object sender, EventArgs e)
        {
            Directions prev_dir = (current_direction == Directions.stop) ? Directions.right : current_direction;
            if (movement.Size != 0)
                current_direction = movement.Remove();
            if (current_direction != Directions.stop)
            {
                int old_head_x = head_x, old_head_y = head_y;
                int old_tail_x = tail_x, old_tail_y = tail_y;

                switch (current_direction)
                {
                    case Directions.up:
                        head_y--;
                        head_dir = Directions.up;
                        tail_move.Add(Directions.down);
                        break;
                    case Directions.down:
                        head_y++;
                        head_dir = Directions.down;
                        tail_move.Add(Directions.up);
                        break;
                    case Directions.right:
                        head_x++;
                        head_dir = Directions.right;
                        tail_move.Add(Directions.left);
                        break;
                    case Directions.left:
                        head_x--;
                        head_dir = Directions.left;
                        tail_move.Add(Directions.right);
                        break;
                    default:
                        break;
                }

                if (head_x == food_x && head_y == food_y)
                {
                    score += 10;
                    ground_info[head_y, head_x] = true;
                    while (true)
                    {
                        food_x = food_location_gen.Next(0, w);
                        food_y = food_location_gen.Next(0, h);
                        if (ground_info[food_y, food_x] == false)
                        {
                            int food_form = food_location_gen.Next(0, 3);
                            ground[food_y, food_x].Image = (food_form == 0) ? (Bitmap)Resources.GetObject("food_banana") : ((food_form == 1) ? (Bitmap)Resources.GetObject("food_orange") : (Bitmap)Resources.GetObject("food_apple"));                            
                            break;
                        }
                    }
                }
                else
                {
                    Directions tail_curr_dir = tail_move.Remove();
                    ground[tail_y, tail_x].Image = (Bitmap)Resources.GetObject("none");
                    ground_info[tail_y, tail_x] = false;
                    switch (tail_curr_dir)
                    {
                        case Directions.up:
                            ++tail_y;
                            break;
                        case Directions.down:
                            --tail_y;
                            break;
                        case Directions.right:
                            --tail_x;
                            break;
                        case Directions.left:
                            ++tail_x;
                            break;
                        default:
                            break;
                    }

                    switch (tail_move.First)
                    {
                        case Directions.up:
                            ground[tail_y, tail_x].Image = (Bitmap)Resources.GetObject("tail__up");
                            break;
                        case Directions.down:
                            ground[tail_y, tail_x].Image = (Bitmap)Resources.GetObject("tail__down");
                            break;
                        case Directions.right:
                            ground[tail_y, tail_x].Image = (Bitmap)Resources.GetObject("tail__right");
                            break;
                        case Directions.left:
                            ground[tail_y, tail_x].Image = (Bitmap)Resources.GetObject("tail__left");
                            break;
                        default:
                            break;
                    }

                    ground_info[tail_y, tail_x] = true;
                    if (head_x == w || head_x == -1 || head_y == -1 || head_y == h || ground_info[head_y, head_x] == true)
                    {
                        timer.Enabled = false;
                        MessageBox.Show("Your score: " + score.ToString() + ".\nYou Lost... :(((");
                        Close();
                        return;
                    }
                }

                ground_info[head_y, head_x] = true;
                switch (head_dir)
                {
                    case Directions.up:
                        ground[head_y, head_x].Image = (Bitmap)Resources.GetObject("head__up");
                        if (prev_dir == Directions.left)
                            ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_upandright");
                        else if (prev_dir == Directions.right)
                            ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_upandleft");
                        break;
                    case Directions.left:
                        ground[head_y, head_x].Image = (Bitmap)Resources.GetObject("head__left");
                        if (prev_dir == Directions.up)
                            ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_downandleft");
                        else if (prev_dir == Directions.down)
                            ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_upandleft");
                        break;
                    case Directions.right:
                        ground[head_y, head_x].Image = (Bitmap)Resources.GetObject("head__right");
                        if (prev_dir == Directions.up)
                            ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_downandright");
                        else if (prev_dir == Directions.down)
                            ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_upandright");
                        break;
                    case Directions.down:
                        ground[head_y, head_x].Image = (Bitmap)Resources.GetObject("head__down");
                        if (prev_dir == Directions.left)
                            ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_downandright");
                        else if (prev_dir == Directions.right)
                            ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_downandleft");
                        break;
                    default:
                        break;
                }

                if (prev_dir == current_direction)
                {
                    if (current_direction == Directions.up || current_direction == Directions.down)
                        ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_vetrical");
                    else if (current_direction == Directions.right || current_direction == Directions.left)
                        ground[old_head_y, old_head_x].Image = (Bitmap)Resources.GetObject("body_horizontal");
                }
            }

            l.Text = "";
            for (int i = 0; i < h; ++i)
            {
                for (int j = 0; j < w; ++j)
                    l.Text += (ground_info[i, j]) ? "o" : "_";

                if (i == 0) l.Text += "\t\tControls: [WASD] or Arrow Keys, [P] for Pause and Resume.";
                if (i == 2) l.Text += "\t\t\"+\" for faster, \"-\" for slower.";
                if (i == 6) l.Text += "\t\tInterval(speed): " + timer.Interval.ToString() + ".";
                if (i == 8) l.Text += "\t\tScore: " + score.ToString() + ".";
                if (i == 4) l.Text += "\t\tTips: don't go out of green field or step on yourself :). GG";
                l.Text += "\n";
            }
        }
    }


    class Node<T>
    {
        public Node<T> next;
        public T value;
    }
    class QueueNode<T>
    {
        Node<T> begin, end, last;
        int size;
        public int Size { get { return size; } }
        public T Last { get { return last.value; } set { last.value = value; } }
        public T First { get { return begin.next.value; } set { begin.next.value = value; } }

        public QueueNode()
        {
            begin = new Node<T>();
            end = new Node<T>();
            begin.next = end;
            last = end;
            size = 0;
        }

        public void Add(T item)
        {
            InnerAdd(ref begin.next, item);
        }

        private void InnerAdd(ref Node<T> n, T item)
        {
            if (n == end)
            {
                n = new Node<T> { value = item };
                n.next = end;
                last = n;
                size++;
                return;
            }
            InnerAdd(ref n.next, item);
        }

        public T Remove()
        {
            T rem_item = begin.next.value;

            begin = begin.next;
            size--;

            return rem_item;
        }

        public void Print()
        {
            Node<T> curr = begin;
            while (curr != end)
            {
                if (curr == begin)
                    Console.Write("{ ");
                else
                    Console.Write(curr.value + " ");

                curr = curr.next;
            }
            Console.WriteLine("}");
        }
    }
}
