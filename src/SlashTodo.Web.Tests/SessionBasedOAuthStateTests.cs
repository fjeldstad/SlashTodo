using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Nancy.Session;
using NUnit.Framework;
using Moq;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class SessionBasedOAuthStateTests
    {
        [Test]
        public void GeneratedStateIsNotEmpty()
        {
            // Arrange
            var session = new Mock<ISession>();
            var oAuthState = new SessionBasedOAuthState(session.Object);

            // Act
            var state = oAuthState.Generate();

            // Assert
            Assert.That(!string.IsNullOrWhiteSpace(state));
        }

        [Test]
        public void GeneratedStateIsStoredInSession()
        {
            // Arrange
            var session = new Mock<ISession>();
            var oAuthState = new SessionBasedOAuthState(session.Object);
            
            // Act
            var state = oAuthState.Generate();

            // Assert
            session.VerifySet(x => x[SessionBasedOAuthState.OAuthStateSessionKey] = state, Times.Once);
        }

        [Test]
        public void GeneratedStateValidatesSuccessfully()
        {
            // Arrange
            var session = new DummySession();
            var oAuthState = new SessionBasedOAuthState(session);
            var state = oAuthState.Generate();

            // Act
            var valid = oAuthState.Validate(state);

            // Assert
            Assert.That(valid, Is.True);
        }

        [Test]
        public void StateDifferentFromTheStoredStateDoesNotValidate()
        {
            // Arrange
            var session = new Mock<ISession>();
            var storedState = "storedState";
            session.SetupGet(x => x[SessionBasedOAuthState.OAuthStateSessionKey]).Returns(storedState);
            var oAuthState = new SessionBasedOAuthState(session.Object);
            var state = "invalidState";
            Assert.That(state, Is.Not.EqualTo(storedState));

            // Act
            var valid = oAuthState.Validate(state);

            // Assert
            Assert.That(valid, Is.False);
        }

        public class DummySession : ISession
        {
            private readonly Dictionary<string, object> _objects = new Dictionary<string, object>();
 
            public int Count
            {
                get { return _objects.Count; }
            }

            public void Delete(string key)
            {
                _objects.Remove(key);
            }

            public void DeleteAll()
            {
                _objects.Clear();
            }

            public bool HasChanged
            {
                get { throw new NotSupportedException(); }
            }

            public object this[string key]
            {
                get { return _objects[key]; }
                set { _objects[key] = value; }
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _objects.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _objects.GetEnumerator();
            }
        }
    }
}
