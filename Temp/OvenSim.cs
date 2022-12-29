using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
namespace Temp
{
    class OvenSim
    {
#if EMULATE 
        private double _heatRate;
        private double _coolRate;
        private double _roomTemp;
        private double _temp;
        private bool _active;
        private Timer ovenTimer;

        public OvenSim(double heatRate, double coolRate, double roomTemp)
        {
            this._heatRate = heatRate;
            this._coolRate  = coolRate;
            this._roomTemp = roomTemp;
            this._temp = roomTemp;
            ovenTimer = new Timer(1000);
            ovenTimer.Elapsed += OvenTimer_Elapsed;
            ovenTimer.Start();
            _active = false;
        }

        private void OvenTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if(_active)
            {
                _temp += _heatRate;
            }
            else
            {
                if(_temp > _roomTemp)
                {
                    _temp -= _coolRate;
                }
                else
                {
                    _temp = _roomTemp;
                }
            }
        }

        public void Start()
        {
            _active = true;
        }
        public void Stop()
        {
            _active = false;
        }

        public double GetTemp()
        {
            return _temp;
        }
    }
#endif
}
