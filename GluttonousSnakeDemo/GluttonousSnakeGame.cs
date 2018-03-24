using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace GluttonousSnakeDemo
{
    /// <summary>
    /// 贪吃蛇游戏类
    /// </summary>
    class GluttonousSnakeGame
    {
        public delegate void MovedEventHandler(object sender, MovedEventArgs gameEventArgs);
        /// <summary>
        /// 当蛇发生移动后发生
        /// </summary>
        public event MovedEventHandler Moved;
        /// <summary>
        /// 获取围墙字符
        /// </summary>
        public const char WallChar = '□';
        /// <summary>
        /// 获取组成蛇身的字符
        /// </summary>
        public const char SnakeChar = '■';
        /// <summary>
        /// 获取食物字符
        /// </summary>
        public const char FoodChar = '●';
        /// <summary>
        /// 获取空地字符
        /// </summary>
        public const char SpaceChar = '　';
        /// <summary>
        /// 速度系数，越大越慢
        /// </summary>
        private const float K = 0.2f;
        /// <summary>
        /// 根据指定的游戏空间尺寸，可选的蛇身最小长度，可选的蛇身最大长度初始化当前对象
        /// </summary>
        /// <param name="placeSize">指定的游戏空间尺寸</param>
        /// <param name="bodyMinLen">指定的蛇身最小长度，默认为 2 </param>
        /// <param name="bodyMaxLen">指定的蛇身最大长度，蛇身长度达到此值时将成功通关，默认为 9 </param>
        public GluttonousSnakeGame(Size placeSize, int bodyMinLen = 2, int bodyMaxLen = 9)
        {
            if (placeSize.Width < 5) throw new ArgumentNullException("PlaceSize", "Width属性不可小于5");
            if (placeSize.Height < 5) throw new ArgumentNullException("PlaceSize", "Height属性不可小于5");
            var spaceSize = placeSize - new Size(2, 2);
            // 效验bodyMinLen bodyMaxLen 两个参数的有效性
            if (bodyMinLen < 1) throw new ArgumentNullException("bodyMinLen", "不可小于1");
            if (spaceSize.Height * spaceSize.Width < bodyMinLen + 1) throw new ArgumentNullException("bodyMinLen", "值不可大于空地的乘积值减一的值");
            if (spaceSize.Height * spaceSize.Width < bodyMaxLen + 1) throw new ArgumentNullException("bodyMaxLen", "值不可大于空地的乘积的值减一的值");
            if (bodyMaxLen <= bodyMinLen) throw new ArgumentNullException("bodyMaxLen", "值不可小于或等于bodyMinLen");

            _map = new char[placeSize.Width, placeSize.Height];
            _placeSize = placeSize;
            _snakeBodyMinLen = bodyMinLen;
            _snakeBodyMaxLen = bodyMaxLen;
            // 初始化地图
            // 首先初始化围墙
            for (int y = 0; y < _placeSize.Height; y++)
            {
                for (int x = 0; x < _placeSize.Width; x++)
                {
                    if (y == 0 || y == _placeSize.Height - 1)
                    {
                        _map[x, y] = WallChar;
                        _wallPos.Add(new Point(x, y));
                    }
                    else if (x == 0 || x == _placeSize.Width - 1)
                    {
                        _map[x, y] = WallChar;
                        _wallPos.Add(new Point(x, y));
                    }
                    else _map[x, y] = SpaceChar;  // 其余位置应使用全角空格填充
                }
            }
            // 初始化蛇身
            for (int i = 0; i < bodyMinLen; i++)
            {
                var column_point = i % spaceSize.Width;
                var row_point = i / spaceSize.Width;
                var temp_point = new Point();
                if (row_point % 2 == 0)   // 如果当前点行数为偶数，规定蛇身坐标从左到右增加
                {
                    temp_point = new Point(column_point, row_point);
                    // 设置初始运动方向向右
                    SetPointDiffVal(MoveDirection.Right);
                }
                else  // 如果当前点行数为奇数，规定蛇身坐标从右到左增加
                {
                    temp_point = new Point(spaceSize.Width - column_point - 1, row_point);
                    // 设置初始运动方向向左
                    SetPointDiffVal(MoveDirection.Left);
                }
                _snakeBodyPos.Add(new Point(temp_point.X + 1, temp_point.Y + 1));
                _map[temp_point.X + 1, temp_point.Y + 1] = SnakeChar;
            }
            _snakeBodyPos.Reverse();    // 反转一下元素，这样第一个元素就是蛇头的坐标
            // 初始化食物位置
            RanSetFoodPoint();
        }
        public void Start()
        {
            _gameing = true;
            stopwatch = new Stopwatch();
            drawThread = new Thread(() =>
            {
                while (_gameing)
                {
                    Flush(MoveBody());   // 移动蛇身并刷新界面
                    Thread.Sleep((int)(1000 * K * _snakeBodyMaxLen / _snakeBodyPos.Count));  // 阻塞一段时间
                }
            });
            drawThread.Start();  // 开启绘制线程
            stopwatch.Start();    // 开始计时
            do
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                switch (keyInfo.Key)
                {
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        if (GetCurMoveDirection() != MoveDirection.Right)
                            SetPointDiffVal(MoveDirection.Left);
                        break;
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        if (GetCurMoveDirection() != MoveDirection.Left)
                            SetPointDiffVal(MoveDirection.Right);
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        if (GetCurMoveDirection() != MoveDirection.Up)
                            SetPointDiffVal(MoveDirection.Down);
                        break;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        if (GetCurMoveDirection() != MoveDirection.Down)
                            SetPointDiffVal(MoveDirection.Up);
                        break;
                    case ConsoleKey.R:
                        break;
                    default:
                        break;
                }
                Thread.Sleep(10);
            } while (_gameing);
        }
        public void Stop()
        {
            _gameing = false;
            stopwatch?.Stop();
            stopwatch = null;
            if (drawThread.IsAlive)
            {
                drawThread.Abort();
                drawThread = null;
            }
            Console.Clear();
        }
        /// <summary>
        /// 移动蛇身，执行结果有四种情况：
        /// <para>0.蛇身的普通移动</para>
        /// <para>1.遇到食物，增加了蛇身长度</para>
        /// <para>2.撞到蛇身或围墙，游戏失败</para>
        /// <para>3.蛇身长度达到指定长度，游戏通关</para>
        /// </summary>
        private MovedEventArgs MoveBody()
        {
            Point last_point = new Point();
            for (int i = 0; i < _snakeBodyPos.Count; i++)
            {
                if (i == 0)
                {
                    var next_point = _snakeBodyPos[0] + new Size(_nextPointDiffVal);    // Point与Point类型之间不能直接相加是个贼坑。。
                    if (_snakeBodyPos.Exists(p => p == next_point) || _wallPos.Exists(p => p == next_point))  // 如果下一点在蛇身或围墙上则终止游戏
                    {
                        return new MovedEventArgs(stopwatch.Elapsed, (_snakeBodyMaxLen - _snakeBodyPos.Count), (_snakeBodyPos.Count - _snakeBodyMinLen), MovedStatus.Fail);
                    }
                    last_point = _snakeBodyPos[0];
                    _snakeBodyPos[0] = next_point;
                }
                else
                {
                    var temp_point = _snakeBodyPos[i];
                    _snakeBodyPos[i] = last_point;
                    last_point = temp_point;
                }
            }
            bool next_point_is_food = _snakeBodyPos[0] == _foodPoint;
            if (next_point_is_food)  // 如果当前点为食物点，则可增加蛇身长度
            {
                _snakeBodyPos.Add(last_point);
                if (_snakeBodyPos.Count != _snakeBodyMaxLen)
                    RanSetFoodPoint();  // UNDONE 重新映射并生成食物 ，如果失败貌似可以不管。。。（因为如果执行失败，那么这蛇就完全盘在一起了，绝对会在下一次循环中撞到自己233）
            }
            // 首先重置地图，映射并生成围墙
            ResetMap();
            // 然后映射蛇身
            foreach (var p in _snakeBodyPos)
            {
                _map[p.X, p.Y] = SnakeChar;
            }
            if (_snakeBodyPos.Count == _snakeBodyMaxLen)
                return new MovedEventArgs(stopwatch.Elapsed, (_snakeBodyMaxLen - _snakeBodyPos.Count), (_snakeBodyPos.Count - _snakeBodyMinLen), MovedStatus.Success); // 当达到指定长度后通关
            return new MovedEventArgs(stopwatch.Elapsed, (_snakeBodyMaxLen - _snakeBodyPos.Count), (_snakeBodyPos.Count - _snakeBodyMinLen), next_point_is_food ? MovedStatus.Food : MovedStatus.Normal);
        }
        /// <summary>
        /// 重置地图，生成围墙和食物
        /// </summary>
        private void ResetMap()
        {
            // 先重置映射数组
            for (int y = 0; y < _placeSize.Height; y++)
            {
                for (int x = 0; x < _placeSize.Width; x++)
                {
                    _map[x, y] = SpaceChar;
                }
            }
            // 生成围墙
            _wallPos.ForEach(p => _map[p.X, p.Y] = WallChar);
            _map[_foodPoint.X, _foodPoint.Y] = FoodChar;
        }
        /// <summary>
        /// 刷新控制台并输出，<see cref="Moved"/>事件由此方法调用
        /// </summary>
        /// <param name="movedEventArgs">蛇身移动后事件参数</param>
        private void Flush(MovedEventArgs movedEventArgs)
        {
            // 首先清空控制台
            Console.Clear();
            for (int y = 0; y < _placeSize.Height; y++)
            {
                for (int x = 0; x < _placeSize.Width; x++)
                {
                    Console.Write(_map[x, y]);
                }
                Console.WriteLine();  // 换行
            }
            Moved?.Invoke(this, movedEventArgs);
        }
        /// <summary>
        /// 随机设置食物位置
        /// </summary>
        private bool RanSetFoodPoint()
        {
            // 返回食物出现位置坐标列表，注意，食物只能生成在空白的位置，如果无法生成，则调用GameOver事件
            var get_space_pos = new Func<Point[]>(() =>
            {
                List<Point> temp_pos = new List<Point>();
                for (int y = 0; y < _placeSize.Height; y++)
                {
                    for (int x = 0; x < _placeSize.Width; x++)
                    {
                        if (!_wallPos.Exists(p => p == new Point(x, y)) && !_snakeBodyPos.Exists(p => p == new Point(x, y)))
                            temp_pos.Add(new Point(x, y));
                    }
                }
                return temp_pos.ToArray();
            });
            // 首先检查全局是否还有全角空格
            var pos = get_space_pos();
            // 如果没有，则调用GameOver事件，并返回（-1，-1）的坐标值
            if (pos.Length == 0)
            {
                return false;
            }
            else // 否则从空格坐标中随机选择出一个点，设置为食物坐标，并刷新地图映射
            {
                var ran_index = new Random().Next(0, pos.Length);
                _foodPoint = pos[ran_index];
                _map[_foodPoint.X, _foodPoint.Y] = FoodChar;
                return true;
            }
        }

        /// <summary>
        /// 获取当前运动方向
        /// </summary>
        /// <returns></returns>
        private MoveDirection GetCurMoveDirection()
        {
            if (_nextPointDiffVal == new Point(-1, 0))
            {
                return MoveDirection.Left;
            }
            else if (_nextPointDiffVal == new Point(1, 0))
            {
                return MoveDirection.Right;
            }
            else if (_nextPointDiffVal == new Point(0, -1))
            {
                return MoveDirection.Up;
            }
            else if (_nextPointDiffVal == new Point(0, 1))
            {
                return MoveDirection.Down;
            }
            else
            {
                return MoveDirection.Right;
            }
        }
        /// <summary>
        /// 设置蛇头运动方向
        /// </summary>
        /// <param name="moveDirection">一个决定移动方向的枚举值</param>
        /// <returns></returns>
        private void SetPointDiffVal(MoveDirection moveDirection)
        {
            // =======蛇头运动方向=======
            //                  (0,-1) 向上
            // (-1,0) 向左                   (1,0) 向右
            //                  (0,1) 向下
            switch (moveDirection)
            {
                case MoveDirection.Left:
                    _nextPointDiffVal = new Point(-1, 0);
                    break;
                case MoveDirection.Right:
                    _nextPointDiffVal = new Point(1, 0);
                    break;
                case MoveDirection.Up:
                    _nextPointDiffVal = new Point(0, -1);
                    break;
                case MoveDirection.Down:
                    _nextPointDiffVal = new Point(0, 1);
                    break;
                default:
                    break;
            }
        }

        bool _gameing;  //标识游戏是否正在运行
        Size _placeSize;
        Point _foodPoint;
        Point _nextPointDiffVal;
        char[,] _map = new char[10, 10];
        Stopwatch stopwatch;
        Thread drawThread; // 专用于绘制的子线程
        List<Point> _snakeBodyPos = new List<Point>();  // 蛇身的坐标集合
        List<Point> _wallPos = new List<Point>();  // 围墙的坐标集合
        int _snakeBodyMaxLen;
        int _snakeBodyMinLen;

        /// <summary>
        /// 蛇移动后事件参数
        /// </summary>
        public class MovedEventArgs : EventArgs
        {
            TimeSpan usedTime;
            int remainTarVal;
            int score;
            MovedStatus status;

            /// <summary>
            /// 获取当前游戏运行时间
            /// </summary>
            public TimeSpan UsedTime => usedTime;
            /// <summary>
            /// 获取当前游戏剩余目标值
            /// </summary>
            public int RemainTarVal => remainTarVal;

            /// <summary>
            /// 获取移动后面临的状态
            /// </summary>
            public MovedStatus Status => status;
            /// <summary>
            /// 获取当前游戏分数
            /// </summary>
            public int Score { get => score; set => score = value; }

            public MovedEventArgs(TimeSpan usedTime, int remainTarVal, int score, MovedStatus status)
            {
                this.usedTime = usedTime;
                this.remainTarVal = remainTarVal;
                this.status = status;
                this.score = score;
            }
        }
        /// <summary>
        /// 标识蛇身移动后的状态
        /// </summary>
        public enum MovedStatus
        {
            /// <summary>
            /// 正常移动
            /// </summary>
            Normal,
            /// <summary>
            /// 遇到食物
            /// </summary>
            Food,
            /// <summary>
            /// 失败
            /// </summary>
            Fail,
            /// <summary>
            /// 成功
            /// </summary>
            Success
        }
        /// <summary>
        /// 蛇头移动方向
        /// </summary>
        enum MoveDirection
        {
            /// <summary>
            /// 向左
            /// </summary>
            Left,
            /// <summary>
            /// 向右
            /// </summary>
            Right,
            /// <summary>
            /// 向上
            /// </summary>
            Up,
            /// <summary>
            /// 向下
            /// </summary>
            Down
        }
    }
}
