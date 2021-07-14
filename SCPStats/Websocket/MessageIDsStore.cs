using System.Collections.Generic;
using System.Threading.Tasks;
using SCPStats.Websocket.Data;

namespace SCPStats.Websocket
{
    internal static class MessageIDsStore
    {
        private static int _warningsCounter = 1000;

        internal static int IncrementWarningsCounter()
        {
            //Bound WarningsCounter to 1000-9999.
            _warningsCounter = (_warningsCounter + 1) % 10000;
            if (_warningsCounter < 1000) _warningsCounter = 1000;

            return _warningsCounter;
        }

        internal static Dictionary<int, TaskCompletionSource<List<Warning>>> WarningsDict = new Dictionary<int, TaskCompletionSource<List<Warning>>>();

        internal static void Reset()
        {
            WarningsDict = new Dictionary<int, TaskCompletionSource<List<Warning>>>();
        }
    }
}