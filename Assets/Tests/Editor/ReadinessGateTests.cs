using NUnit.Framework;

namespace AtMycelia.Myceliarium.Tests
{
    public class ReadinessGateTests
    {
        private ReadinessGate _gate;
        private int _calls;

        [SetUp]
        public void SetUp()
        {
            _gate = new ReadinessGate();
            _calls = 0;
        }

        [Test]
        public void IsReady_BeforeAnyPassCompletes_IsFalse()
        {
            Assert.That(_gate.IsReady, Is.False);

            _gate.TryBeginPass();

            Assert.That(_gate.IsReady, Is.False);
        }

        [Test]
        public void IsReady_AfterAPassEnds_IsTrue()
        {
            _gate.TryBeginPass();
            _gate.EndPass();

            Assert.That(_gate.IsReady, Is.True);
        }

        [Test]
        public void IsReady_WhileANewPassIsPending_IsFalse()
        {
            _gate.TryBeginPass();
            _gate.EndPass();

            _gate.TryBeginPass();

            Assert.That(_gate.IsReady, Is.False);
        }

        [Test]
        public void TryBeginPass_WhileOneIsPending_ReturnsFalse()
        {
            Assert.That(_gate.TryBeginPass(), Is.True);
            Assert.That(_gate.TryBeginPass(), Is.False);
        }

        [Test]
        public void WhenReady_BeforeReady_WaitsForThePassToEnd()
        {
            _gate.TryBeginPass();
            _gate.RunWhenReady(() => _calls++);

            Assert.That(_calls, Is.EqualTo(0));

            _gate.EndPass();

            Assert.That(_calls, Is.EqualTo(1));
        }

        [Test]
        public void WhenReady_WhenAlreadyReady_RunsRightAway()
        {
            _gate.TryBeginPass();
            _gate.EndPass();

            _gate.RunWhenReady(() => _calls++);

            Assert.That(_calls, Is.EqualTo(1));
        }

        [Test]
        public void WhenReady_RunsEachActionOnlyOnce()
        {
            _gate.TryBeginPass();
            _gate.RunWhenReady(() => _calls++);
            _gate.EndPass();

            _gate.TryBeginPass();
            _gate.EndPass();

            Assert.That(_calls, Is.EqualTo(1));
        }

        [Test]
        public void WhenReady_WithANewPassPending_WaitsForThatOneToo()
        {
            _gate.TryBeginPass();
            _gate.EndPass();
            _gate.TryBeginPass(); // The second pass that replaces the first's results

            _gate.RunWhenReady(() => _calls++);
            Assert.That(_calls, Is.EqualTo(0));

            _gate.EndPass();
            Assert.That(_calls, Is.EqualTo(1));
        }

        [Test]
        public void WhenReady_CalledFromWithinAnAction_DoesNotLoseOrRepeatActions()
        {
            _gate.TryBeginPass();
            _gate.RunWhenReady(() => _gate.RunWhenReady(() => _calls++));

            _gate.EndPass();

            Assert.That(_calls, Is.EqualTo(1));
        }
    }
}
