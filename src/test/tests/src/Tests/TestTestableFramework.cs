using System;
using NUnit.Framework;
using Ninject;
using Testable;

namespace Tests {
    [TestFixture()]
    public class TestTestableFramework : BaseInjectedTest {

        [Testable.GameObjectBoundary]
        public class MockComponent : Testable.TestableComponent {
            public MockComponent(TestableGameObject obj) : base(obj) {
            }

            public int updateCount { get; private set; }
            
            public override void Update() {
                updateCount++;
            }
        }

        public class HasMultipleGameObjects {
            public TestableGameObject a { get; private set; }
            public TestableGameObject b { get; private set; }
            public HasMultipleGameObjects(TestableGameObject a, TestableGameObject b) {
                this.a = a;
                this.b = b;
            }
        }

        [Testable.GameObjectBoundary]
        public class HasGameObjectBoundaryAsParameter : Testable.TestableComponent {
            public MockComponent nested { get; private set; }
            public HasGameObjectBoundaryAsParameter(TestableGameObject obj, MockComponent nested) : base(obj) {
                this.nested = nested;
                this.nested.Obj.transform.Parent = this.Obj.transform;
            }
        }

        [Testable.GameObjectBoundary]
        public class HasInjectedPrefab : Testable.TestableComponent {
            public TestableGameObject nested { get; private set; }
            public HasInjectedPrefab(TestableGameObject parent, [PrefabAttribute("Sphere")] TestableGameObject nested) : base(parent) {
                this.nested = nested;
                nested.transform.Parent = this.Obj.transform;
            }
        }

        [Test()]
        public void TestTestableComponentIsUpdated() {
            MockComponent component = kernel.Get<MockComponent>();

            Assert.AreEqual(0, component.updateCount);
            step(1);
            Assert.AreEqual(1, component.updateCount);
        }

        [Test]
        public void testNestedTopLevelGameObjectsGetDifferentGameObjects() {
            HasGameObjectBoundaryAsParameter foo = kernel.Get<HasGameObjectBoundaryAsParameter>();
            Assert.AreNotSame(foo.Obj, foo.nested.Obj);
        }

        [Test]
        public void testInjectedPrefabHasDistinctTransform() {
            HasInjectedPrefab prefab = kernel.Get<HasInjectedPrefab>();
            Assert.AreNotEqual(prefab.Obj, prefab.nested);
            Assert.AreEqual(prefab.Obj.transform, prefab.nested.transform.Parent);
            Assert.IsNull(prefab.Obj.transform.Parent);
        }

        [Test]
        public void testResources() {
            IResourceLoader loader = kernel.Get<IResourceLoader>();
            Assert.AreEqual("Hello World", loader.loadDoc("test").Element("element").Value);
        }

        [Test]
        public void testLayerMasksInterpreted() {
            ILayerMask layerMask = kernel.Get<ILayerMask>();
            Assert.AreEqual(0, layerMask.NameToLayer("Default"));
        }

        [Test]
        public void testPrefabLoading() {
            Assert.IsNotNull(kernel.Get<IResourceLoader>().instantiate("Sphere"));
        }

        /// <summary>
        /// Our testable example should create exactly two game objects; one for the component itself,
        /// and another for its injected prefab.
        /// </summary>
        [Test]
        public void testTestableSceneObjectCreationCount() {
            kernel.Get<TestableExample>();
            step(1); // Must step a frame to ensure our test updatable manager tracks all objects.
            Assert.AreEqual(2, kernel.Get<TestUpdatableManager>().Count);
        }
    }
}


