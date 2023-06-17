using System;

namespace EventHandlerLeakAnalyzer.Test.TestFiles.TestCode3
{
    public static class StaticClass
    {
        public static event Action StaticEvent;
    }

    class TestClass
    {
        private void OnEnable()
        {
            // Should not trigger a diagnostic here since we unsubscribe below
            StaticClass.StaticEvent += Response;
        }

        private void Response()
        {
        }

        private void OnDisable()
        {
            StaticClass.StaticEvent -= Response;
        }
    }
}