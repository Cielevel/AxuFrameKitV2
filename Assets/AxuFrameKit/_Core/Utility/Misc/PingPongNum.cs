
using System;

namespace AxuFrameKit._Core.Utility.Misc
{
    /// <summary>
    /// 乒乓数字 - 在指定区间内来回移动的数值
    /// </summary>
    public class PingPongNum
    {
        public int Value { get; private set; }

        public int Min => _min;

        public int Max => _max;

        public Dir Direction => _dir;
        
        private int DirNum => _dir == Dir.Positive ? 1 : -1;
        
        private Dir _dir;

        private readonly Dir _initDir;
        
        private readonly int _min, _max, _step, _initValue;

        /// <summary>
        /// 乒乓数字初始化
        /// </summary>
        /// <param name="min">最小值（闭区间）</param>
        /// <param name="max">最大值（闭区间）</param>
        /// <param name="value">初始值</param>
        /// <param name="dir">初始方向</param>
        /// <param name="step">步进</param>
        public PingPongNum(int min, int max, int value, Dir dir, int step = 1)
        {
            // 参数验证 - 使用异常而不是日志+return
            if (min > max)
                throw new ArgumentException($"最小值 ({min}) 不能大于最大值 ({max})", nameof(min));

            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(nameof(value), $"初始值 ({value}) 必须在区间 [{min}, {max}] 内");

            if (step <= 0)
                throw new ArgumentException("步进值必须为正数", nameof(step));

            _dir     = dir;
            _step    = step;
            _max     = max;
            _min     = min;
            _initDir = dir;

            Value = value;

            _initValue = value;
        }

        public void Move()
        {
            Value += _step * DirNum;

            if (Value >= _max)
            {
                Value = _max;
                _dir  = Dir.Negative;
            }
            else if (Value <= _min)
            {
                Value = _min;
                _dir  = Dir.Positive;
            }
        }

        /// <summary>
        /// 设置为指定值和方向
        /// </summary>
        /// <param name="value">新值</param>
        /// <param name="dir">新方向</param>
        /// <exception cref="ArgumentOutOfRangeException">值超出范围时抛出</exception>
        public void SetValueAndDirection(int value, Dir dir)
        {
            if (value < _min || value > _max)
                throw new ArgumentOutOfRangeException(nameof(value), $"值 ({value}) 必须在区间 [{_min}, {_max}] 内");

            Value = value;
            _dir  = dir;
        }
        
        /// <summary>
        /// 获取下一个值（不改变当前状态）
        /// </summary>
        /// <returns>下一次Move()会得到的值</returns>
        public int PeekNext()
        {
            int nextValue = Value + _step * DirNum;
            
            if (nextValue >= _max)
                return _max;
            if (nextValue <= _min)
                return _min;
            
            return nextValue;
        }
        
        /// <summary>
        /// 反转方向
        /// </summary>
        public void ReverseDirection()
        {
            _dir = _dir == Dir.Positive ? Dir.Negative : Dir.Positive;
        }
        
        public void Reset()
        {
            Value = _initValue;
            _dir  = _initDir;
        }

        public enum Dir
        {
            Negative, 
            Positive
        }
        
        public override string ToString()
        {
            return $"PingPongNum[Value={Value}, Range=[{_min},{_max}], Direction={_dir}, Step={_step}]";
        }
    }
}
