﻿using System.Collections;
using System.Collections.Generic;
using GameAI;
using NUnit.Framework;
using Unity.Entities;
using Unity.Entities.PerformanceTests;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace PerfTests
{
    [TestFixture]
    [Category("Performance")]
    public sealed class ECSPerfTestExample : EntityPerformanceTestFixture
    {
        [Test, Performance]
        public void WorldCreatorMeasure([Values(10, 100)] int x, [Values(10, 100)] int y)
        {
            var creator = m_World.GetOrCreateSystem<WorldCreatorSystem>();
            creator.WorldSize = new int2(x, y);
            
            Measure.Method(() =>
            {
                //Assert.IsTrue(creator.Enabled);
                //Assert.IsTrue(creator.ShouldRunSystem());
                creator.Update();    
            })
                .SetUp(() =>
                {
                    var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    var entities = entityManager.GetAllEntities();
                    entityManager.DestroyEntity(entities);
                    entities.Dispose();

                    WorldCreatorSystem.ResetExecuteOnceTag(m_Manager);
                    RenderingMapInit.ResetExecuteOnceTag(m_Manager);
                })
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }

        [Test, Performance]
        public void WorldMapInitMeasure([Values(10, 100)] int x, [Values(10, 100)] int y)
        {
            var creator = m_World.GetOrCreateSystem<WorldCreatorSystem>();
            creator.WorldSize = new int2(x, y);
            
            var barrier = m_World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            var mapInit = m_World.GetOrCreateSystem<RenderingMapInit>();

            Measure.Method(() =>
                {
                    mapInit.Update();
                    barrier.Update(); // Playback happens in the update of the system that owns the command buffer.
                    m_Manager.CompleteAllJobs();
                    
                })
                .SetUp(() =>
                {
                    m_Manager.CompleteAllJobs();
                    
                    // kill all entities
                    var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    var entities = entityManager.GetAllEntities();
                    entityManager.DestroyEntity(entities);
                    entities.Dispose();

                    // recreate entities
                    var ru = RenderingUnity.instance;
                    ru.Initialize();

                    mapInit.CreatePrefabEntity();

                    WorldCreatorSystem.ResetExecuteOnceTag(m_Manager);
                    creator.Update();

                    RenderingMapInit.ResetExecuteOnceTag(m_Manager);

                    Assert.IsTrue(mapInit.Enabled, "mapInit.Enabled");
                    Assert.IsTrue(barrier.Enabled, "barrier.Enabled");
                    Assert.IsTrue(mapInit.ShouldRunSystem(), "mapInit.ShouldRunSystem()");
                    Assert.IsTrue(barrier.ShouldRunSystem(), "barrier.ShouldRunSystem()");
                })
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }

        [Test, Performance]
        public void DemoPerfTest([Values(1, 10, 100)] int n, [Values(100, 1000)] int entityCount)
        {
            var atype = m_Manager.CreateArchetype(typeof(Translation), typeof(LocalToWorld), typeof(RenderMesh));

            Measure.Method(() =>
                {
                    for (int i = 0; i < n; ++i)
                    {
                        m_Manager.CreateEntity(atype);
                    }
                })
                .Definition("DemoTest")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }
    }
}