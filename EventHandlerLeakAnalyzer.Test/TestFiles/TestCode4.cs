using System;

namespace EventHandlerLeakAnalyzer.Test.TestFiles.TestCode4
{
    public static class StaticClass
    {
        public static event Action StaticEvent;
        public static event Action StaticEventDummy;
    }

    public static class StaticClassDummy
    {
        public static event Action StaticEvent;
    }

    class TestClass
    {
        private void OnEnable()
        {
            // Should trigger one diagnostic here because we don't subscribe this response from this event
            StaticClass.StaticEvent += Response;
        }

        private void Response()
        {
        }

        private void ResponseDummy()
        {
            
        }

        private void OnDisable()
        {
            StaticClass.StaticEvent -= ResponseDummy;
            StaticClass.StaticEventDummy -= Response;
            StaticClassDummy.StaticEvent -= Response;
        }
    }
}