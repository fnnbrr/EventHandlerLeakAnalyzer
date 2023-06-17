using System;

namespace EventHandlerLeakAnalyzer.Test.TestFiles.TestCode2
{
    public static class StaticClass
    {
        public static event Action StaticEvent;
    }

    class TestClass
    {
        private void OnEnable()
        {
            // Should trigger one diagnostic here
            StaticClass.StaticEvent += Response;
        }

        private void Response()
        {
        }
    }
}