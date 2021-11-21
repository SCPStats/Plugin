using System.Collections.Generic;
using System.Threading.Tasks;
using SCPStats.Websocket.Data;

namespace SCPStats.Websocket
{
    internal static class MessageIDsStore
    {
        private static int _warningsCounter = 1000;
        private static int _warnCounter = 1000;
        private static int _delwarnCounter = 1000;

        internal static int IncrementWarningsCounter()
        {
            //Bound WarningsCounter to 1000-9999.
            _warningsCounter = (_warningsCounter + 1) % 10000;
            if (_warningsCounter < 1000) _warningsCounter = 1000;

            return _warningsCounter;
        }

        internal static Dictionary<int, TaskCompletionSource<List<Warning>>> WarningsDict = new Dictionary<int, TaskCompletionSource<List<Warning>>>();
        
        internal static int IncrementWarnCounter()
        {
            //Bound WarnCounter to 1000-9999.
            _warnCounter = (_warnCounter + 1) % 10000;
            if (_warnCounter < 1000) _warnCounter = 1000;

            return _warnCounter;
        }

        internal static Dictionary<int, TaskCompletionSource<bool>> WarnDict = new Dictionary<int, TaskCompletionSource<bool>>();
        
        internal static int IncrementDelWarnCounter()
        {
            //Bound WarnCounter to 1000-9999.
            _delwarnCounter = (_delwarnCounter + 1) % 10000;
            if (_delwarnCounter < 1000) _delwarnCounter = 1000;

            return _delwarnCounter;
        }

        internal static Dictionary<int, TaskCompletionSource<bool>> DelwarnDict = new Dictionary<int, TaskCompletionSource<bool>>();

        internal static void Reset()
        {
            WarningsDict = new Dictionary<int, TaskCompletionSource<List<Warning>>>();
        }
    }
}