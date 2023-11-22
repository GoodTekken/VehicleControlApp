using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace VehicleControlApp
{
    internal class PositionData : INotifyPropertyChanged
    {
        private int? angle;
        private int? angle2;
        private int? navLevel;
        private int? valid;
        private int? x;
        private int? y;
        private int? reqExtSegment;
        private int? reqSegmentId;
        protected DateTime m_CreatedUTC;


        public int? Angle
        {
            get => angle;
            set
            {
                angle = value;
                OnPropertyChanged(nameof(Angle));
            }
        }

        public int? Angle2
        {
            get => angle2;
            set
            {
                angle2 = value;
                OnPropertyChanged(nameof(Angle2));
            }
        }

        public int? NavLevel
        {
            get => navLevel;
            set
            {
                navLevel = value;
                OnPropertyChanged(nameof(navLevel));
            }
        }

        public int? Valid
        {
            get => valid;
            set
            {
                valid = value;
                OnPropertyChanged(nameof(valid));
            }
        }

        public int? X
        {
            get => x;
            set
            {
                x = value;
                OnPropertyChanged(nameof(x));
            }
        }

        public int? Y
        {
            get => y;
            set
            {
                y = value;
                OnPropertyChanged(nameof(y));
            }
        }

        public int? ReqExtSegment
        {
            get => reqExtSegment;
            set
            {
                reqExtSegment = value;
                OnPropertyChanged(nameof(reqExtSegment));
            }
        }
        public int? ReqSegmentId
        {
            get => reqSegmentId;
            set
            {
                reqSegmentId = value;
                OnPropertyChanged(nameof(reqSegmentId));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DateTime CreatedUTC
        {
            get
            {
                return m_CreatedUTC;
            }
            set
            {
                m_CreatedUTC = value;
            }
        }

        public override string ToString()
        {
            CreatedUTC = DateTime.Now;
            return string.Format("Angle:{0}, Angle2:{1}, NavLevel:{2}, Valid:{3}, X:{4}, Y:{5}, ReqExtSegment:{6}, ReqSegmentId:{7} - {8}",
                Angle,
                Angle2,
                NavLevel,
                Valid,
                X,
                Y,
                ReqExtSegment,
                ReqSegmentId,
                CreatedUTC.ToString("yyyy-MM-dd HH:mm:ss:fff"));
        }
    }
}
