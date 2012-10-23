using System;
using NUnit.Framework;
using Ninject;
using Uniject;
using UnityEngine;
using System.IO;
using System.Xml.XPath;

namespace Tests {

    /// <summary>
    /// Tests for Uniject.
    /// </summary>
    [TestFixture()]
    public class TestUniject : BaseInjectedTest {

        [GameObjectBoundary]
        public class MockComponent : TestableComponent {
            public MockComponent(TestableGameObject obj) : base(obj) {
            }

            public int updateCount { get; private set; }
            
            public override void Update() {
                updateCount++;
            }
        }

        [GameObjectBoundary]
        public class HasInjectedGameObjects {
            public TestableGameObject a { get; private set; }
            public TestableGameObject b { get; private set; }
            public HasInjectedGameObjects([GameObjectBoundary] TestableGameObject a, [GameObjectBoundary] TestableGameObject b) {
                this.a = a;
                this.b = b;
            }
        }

        [GameObjectBoundary]
        public class HasGameObjectBoundaryAsParameter : TestableComponent {
            public MockComponent nested { get; private set; }
            public HasGameObjectBoundaryAsParameter(TestableGameObject obj, MockComponent nested) : base(obj) {
                this.nested = nested;
                this.nested.Obj.transform.Parent = this.Obj.transform;
            }
        }

        [GameObjectBoundary]
        public class HasInjectedPrefab : TestableComponent {
            public TestableGameObject nested { get; private set; }
            public HasInjectedPrefab(TestableGameObject parent, [Resource("mesh/sphere")] TestableGameObject nested) : base(parent) {
                this.nested = nested;
                nested.transform.Parent = this.Obj.transform;
            }
        }

        /// <summary>
        /// Tests the testable component has its Update method called.
        /// </summary>
        [Test()]
        public void TestTestableComponentIsUpdated() {
            MockComponent component = kernel.Get<MockComponent>();

            Assert.AreEqual(0, component.updateCount);
            step(1);
            Assert.AreEqual(1, component.updateCount);
        }

        [Test]
        public void TestDestroyedObjectsNotUpdated() {
            MockComponent component = kernel.Get<MockComponent>();
            component.Obj.Destroy();
            step();
            Assert.AreEqual(0, component.updateCount);
        }

        /// <summary>
        /// Components that feature the <code>GameObjectBoundary</code> attribute
        /// should get their own GameObject if injected as a dependency.
        /// </summary>
        [Test]
        public void testNestedTopLevelGameObjectsGetDifferentGameObjects() {
            HasGameObjectBoundaryAsParameter foo = kernel.Get<HasGameObjectBoundaryAsParameter>();
            Assert.AreNotSame(foo.Obj, foo.nested.Obj);
        }

        /// <summary>
        /// Prefabs that are injected into components should have their own transforms,
        /// not share those of their parent.
        /// </summary>
        [Test]
        public void testInjectedPrefabHasDistinctTransform() {
            HasInjectedPrefab prefab = kernel.Get<HasInjectedPrefab>();
            Assert.AreNotEqual(prefab.Obj, prefab.nested);
            Assert.AreEqual(prefab.Obj.transform, prefab.nested.transform.Parent);
            Assert.IsNull(prefab.Obj.transform.Parent);
        }

        /// <summary>
        /// Tries to load a string from an XML file in the resources folder.
        /// </summary>
        [Test]
        public void testResources() {
            IResourceLoader loader = kernel.Get<IResourceLoader>();
            Assert.AreEqual("Hello World", loader.loadDoc("xml/test").XPathSelectElement("root/string").Value);
        }

        [Test]
        public void testLayerMasksInterpreted() {
            ILayerMask layerMask = kernel.Get<ILayerMask>();
            Assert.AreEqual(0, layerMask.NameToLayer("Default"));
        }

        [Test]
        public void testPrefabLoading() {
            Assert.IsNotNull(kernel.Get<IResourceLoader>().instantiate("mesh/sphere"));
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

        /// <summary>
        /// Injected raw <c>TestableGameObject</c>.
        /// </summary>
        [Test]
        public void testHasInjectedObjects() {
            HasInjectedGameObjects injected = kernel.Get<HasInjectedGameObjects>();
            Assert.AreNotEqual(injected.a, injected.b);
        }

        private class HasAttributedAudioClip {
            public AudioClip clip { get; private set; }
            public HasAttributedAudioClip([Resource("audio/beep")] AudioClip clip) {
                this.clip = clip;
            }
        }

        [Test]
        public void testAttributedAudioClipLoads() {
            Assert.IsNotNull(kernel.Get<HasAttributedAudioClip>());
        }

        [Test]
        public void testMissingAttributedAudioClipErrors() {
            try {
                kernel.Get<HasMissingAttributedAudioClip>();
                Assert.Fail();
            } catch (FileNotFoundException) {
            }
        }

        private class HasMissingAttributedAudioClip {
            public HasMissingAttributedAudioClip([Resource("does/not/exist")] AudioClip clip) {
            }
        }

        private class HasMissingAttributedPrefab {
            public HasMissingAttributedPrefab([Resource("does/not/exist")] TestableGameObject obj) { }
        }

        [Test]
        public void testMissingAttributedPrefabErrors() {
            try {
                kernel.Get<HasMissingAttributedPrefab>();
                Assert.Fail();
            } catch (FileNotFoundException) {
            }
        }

        private class HasPhysicMaterial {
            public HasPhysicMaterial([Resource("physic/bouncy")] IPhysicMaterial mat) {
            }
        }

        [Test]
        public void testHasPhysicMaterial() {
            kernel.Get<HasPhysicMaterial>();
        }

        private class HasNonExistentPhysicMaterial {
            public HasNonExistentPhysicMaterial([Resource("does/not/exist")] IPhysicMaterial mat) {
            }
        }

        [Test]
        public void testHasNonExistentPhysicMaterial() {
            try {
                kernel.Get<HasNonExistentPhysicMaterial>();
                Assert.Fail();
            } catch (FileNotFoundException) {
            }
        }

        [Test]
        public void testOnCollisionEnterCalled() {
            HasOnCollision has = kernel.Get<HasOnCollision>();
            Assert.IsFalse(has.onCollisionEnterCalled);
            has.Obj.OnCollisionEnter(new Uniject.Collision());
            Assert.IsTrue(has.onCollisionEnterCalled);
        }

        public class HasOnCollision : TestableComponent {
            public HasOnCollision(TestableGameObject obj) : base(obj) { }
            public bool onCollisionEnterCalled { get; private set; }
            public override void OnCollisionEnter(Uniject.Collision collision) {
                onCollisionEnterCalled = true;
            }
        }
    }
}


