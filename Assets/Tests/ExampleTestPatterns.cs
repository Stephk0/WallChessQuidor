using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace WallChess.Tests
{
    /// <summary>
    /// Example test patterns demonstrating correct Unity Test Framework usage.
    /// This file serves as a reference for writing tests in the project.
    /// </summary>
    [TestFixture]
    public class ExampleTestPatterns
    {
        #region Test Setup and Teardown
        
        private GameObject testContainer;
        
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Runs once before all tests in this class
            Debug.Log("Setting up test fixture");
        }
        
        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            // Runs once after all tests in this class
            Debug.Log("Tearing down test fixture");
        }
        
        [SetUp]
        public void Setup()
        {
            // Runs before each test
            testContainer = new GameObject("TestContainer");
        }
        
        [TearDown]
        public void TearDown()
        {
            // Runs after each test
            if (testContainer != null)
            {
                Object.DestroyImmediate(testContainer);
            }
        }
        
        #endregion
        
        #region Basic Test Examples
        
        /// <summary>
        /// Simple synchronous test - runs immediately
        /// </summary>
        [Test]
        [Category("Unit")]
        public void SimpleTest_BasicAssertion_Passes()
        {
            // Arrange
            int expected = 5;
            int actual = 2 + 3;
            
            // Act & Assert
            Assert.AreEqual(expected, actual, "Basic math should work");
        }
        
        /// <summary>
        /// Test with multiple assertions
        /// </summary>
        [Test]
        [Category("Unit")]
        public void GameObject_Creation_HasCorrectDefaults()
        {
            // Arrange & Act
            var go = new GameObject("TestObject");
            
            // Assert
            Assert.IsNotNull(go, "GameObject should be created");
            Assert.AreEqual("TestObject", go.name, "Name should match");
            Assert.IsTrue(go.activeSelf, "Should be active by default");
            Assert.AreEqual(Vector3.zero, go.transform.position, "Should be at origin");
            
            // Cleanup
            Object.DestroyImmediate(go);
        }
        
        #endregion
        
        #region Unity Test Examples (Coroutines)
        
        /// <summary>
        /// UnityTest for testing over multiple frames
        /// </summary>
        [UnityTest]
        [Category("Integration")]
        public IEnumerator UnityTest_WaitForFrames_ExecutesCorrectly()
        {
            // Arrange
            int frameCount = 0;
            
            // Act - Wait for 3 frames
            for (int i = 0; i < 3; i++)
            {
                frameCount++;
                yield return null; // Wait one frame
            }
            
            // Assert
            Assert.AreEqual(3, frameCount, "Should have waited for 3 frames");
        }
        
        /// <summary>
        /// UnityTest with time-based waiting
        /// </summary>
        [UnityTest]
        [Category("Integration")]
        [Timeout(5000)] // Timeout after 5 seconds
        public IEnumerator UnityTest_WaitForTime_CompletesWithinTimeout()
        {
            // Arrange
            float startTime = Time.realtimeSinceStartup;
            float waitTime = 0.5f;
            
            // Act
            yield return new WaitForSeconds(waitTime);
            
            // Assert
            float elapsed = Time.realtimeSinceStartup - startTime;
            Assert.GreaterOrEqual(elapsed, waitTime, "Should have waited at least the specified time");
            Assert.Less(elapsed, waitTime + 0.1f, "Should not wait significantly longer");
        }
        
        #endregion
        
        #region Component Testing
        
        /// <summary>
        /// Test adding and configuring components
        /// </summary>
        [Test]
        [Category("Components")]
        public void Component_AddAndConfigure_WorksCorrectly()
        {
            // Arrange
            var go = new GameObject("ComponentTest");
            
            // Act
            var collider = go.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 2f;
            collider.isTrigger = true;
            
            // Assert
            Assert.IsNotNull(collider, "Component should be added");
            Assert.AreEqual(Vector3.one * 2f, collider.size, "Size should be set correctly");
            Assert.IsTrue(collider.isTrigger, "Trigger should be enabled");
            
            // Cleanup
            Object.DestroyImmediate(go);
        }
        
        #endregion
        
        #region Parameterized Tests
        
        /// <summary>
        /// Test with multiple test cases using TestCase attribute
        /// </summary>
        [Test]
        [Category("Math")]
        [TestCase(0, 0, 0)]
        [TestCase(1, 1, 2)]
        [TestCase(-1, 1, 0)]
        [TestCase(10, -5, 5)]
        public void Addition_VariousInputs_ProducesCorrectResult(int a, int b, int expected)
        {
            // Act
            int result = a + b;
            
            // Assert
            Assert.AreEqual(expected, result, $"{a} + {b} should equal {expected}");
        }
        
        /// <summary>
        /// Test with value source for more complex test data
        /// </summary>
        [Test]
        [Category("Validation")]
        public void Vector_Normalization_ProducesUnitVector([ValueSource(nameof(VectorTestCases))] Vector3 input)
        {
            // Skip zero vector as it cannot be normalized
            if (input == Vector3.zero) 
            {
                Assert.Pass("Zero vector cannot be normalized");
                return;
            }
            
            // Act
            Vector3 normalized = input.normalized;
            
            // Assert
            Assert.AreEqual(1f, normalized.magnitude, 0.0001f, "Normalized vector should have magnitude of 1");
        }
        
        private static Vector3[] VectorTestCases = new Vector3[]
        {
            Vector3.right,
            Vector3.up,
            Vector3.forward,
            new Vector3(3, 4, 0),
            new Vector3(1, 1, 1),
            Vector3.zero
        };
        
        #endregion
        
        #region Exception Testing
        
        /// <summary>
        /// Test that exceptions are thrown correctly
        /// </summary>
        [Test]
        [Category("ErrorHandling")]
        public void InvalidOperation_ThrowsExpectedException()
        {
            // Arrange
            GameObject nullObject = null;
            
            // Act & Assert
            Assert.Throws<System.NullReferenceException>(() =>
            {
                var name = nullObject.name; // This should throw
            }, "Should throw NullReferenceException when accessing null object");
        }
        
        #endregion
        
        #region Performance Testing
        
        /// <summary>
        /// Test with performance constraints
        /// </summary>
        [Test]
        [Category("Performance")]
        [MaxTime(100)] // Test must complete within 100ms
        public void PerformanceTest_QuickOperation_CompletesInTime()
        {
            // Arrange
            var objects = new GameObject[100];
            
            // Act
            for (int i = 0; i < 100; i++)
            {
                objects[i] = new GameObject($"PerfTest_{i}");
            }
            
            // Assert (implicit - test passes if it completes in time)
            
            // Cleanup
            foreach (var obj in objects)
            {
                Object.DestroyImmediate(obj);
            }
        }
        
        #endregion
        
        #region Conditional Tests
        
        /// <summary>
        /// Test that only runs under certain conditions
        /// </summary>
        [Test]
        [Category("Platform")]
        [Platform("Unity")] // Only runs in Unity
        public void PlatformSpecific_UnityOnly_Test()
        {
            Assert.IsTrue(Application.isEditor || Application.isPlaying, 
                "Should be running in Unity");
        }
        
        /// <summary>
        /// Test that can be ignored with a reason
        /// </summary>
        [Test]
        [Ignore("Not implemented yet - waiting for feature X")]
        public void FutureFeature_NotYetImplemented()
        {
            // This test will be skipped
        }
        
        #endregion
        
        #region Custom Assertions
        
        /// <summary>
        /// Example of custom assertion methods
        /// </summary>
        [Test]
        [Category("CustomAssertions")]
        public void CustomAssertion_Example()
        {
            // Arrange
            var position = new Vector3(1.0001f, 2.0002f, 3.0003f);
            var expected = new Vector3(1f, 2f, 3f);
            
            // Act & Assert
            AssertVector3Approximately(expected, position, 0.001f);
        }
        
        private void AssertVector3Approximately(Vector3 expected, Vector3 actual, float tolerance)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance, $"X component mismatch");
            Assert.AreEqual(expected.y, actual.y, tolerance, $"Y component mismatch");
            Assert.AreEqual(expected.z, actual.z, tolerance, $"Z component mismatch");
        }
        
        #endregion
        
        #region LogAssert Examples
        
        /// <summary>
        /// Test Unity console output
        /// </summary>
        [Test]
        [Category("Logging")]
        public void LogTest_ExpectedWarning_IsLogged()
        {
            // Arrange & Act
            LogAssert.Expect(LogType.Warning, "This is a test warning");
            Debug.LogWarning("This is a test warning");
            
            // Assert is implicit - test fails if expected log doesn't occur
        }
        
        /// <summary>
        /// Test that no errors occur
        /// </summary>
        [Test]
        [Category("Logging")]
        public void Operation_NoErrors_Succeeds()
        {
            // Arrange
            LogAssert.NoUnexpectedReceived();
            
            // Act
            var go = new GameObject("TestObject");
            go.transform.position = Vector3.one;
            
            // Assert - test fails if any errors were logged
            
            // Cleanup
            Object.DestroyImmediate(go);
        }
        
        #endregion
    }
}