﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Barotrauma
{
    class BackgroundCreatureManager
    {
        const int MaxSprites = 100;

        const float CheckActiveInterval = 1.0f;

        private float checkActiveTimer;

        private List<BackgroundCreaturePrefab> prefabs = new List<BackgroundCreaturePrefab>();
        private List<BackgroundCreature> activeSprites = new List<BackgroundCreature>();

        public BackgroundCreatureManager(string configPath)
        {
            LoadConfig(configPath);
        }
        public BackgroundCreatureManager(List<string> files)
        {
            foreach(var file in files)
            {
                LoadConfig(file);
            }
        }
        private void LoadConfig(string configPath)
        {
            try
            {
                XDocument doc = XMLExtensions.TryLoadXml(configPath);
                if (doc == null || doc.Root == null) return;

                foreach (XElement element in doc.Root.Elements())
                {
                    prefabs.Add(new BackgroundCreaturePrefab(element));
                };
            }
            catch (Exception e)
            {
                DebugConsole.ThrowError(String.Format("Failed to load BackgroundCreatures from {0}", configPath), e);
            }
        }
        public void SpawnSprites(int count, Vector2? position = null)
        {
            activeSprites.Clear();

            if (prefabs.Count == 0) return;

            count = Math.Min(count, MaxSprites);

            for (int i = 0; i < count; i++ )
            {
                Vector2 pos = Vector2.Zero;

                if (position == null)
                {
                    var wayPoints = WayPoint.WayPointList.FindAll(wp => wp.Submarine==null);
                    if (wayPoints.Any())
                    {
                        WayPoint wp = wayPoints[Rand.Int(wayPoints.Count, Rand.RandSync.ClientOnly)];

                        pos = new Vector2(wp.Rect.X, wp.Rect.Y);
                        pos += Rand.Vector(200.0f, Rand.RandSync.ClientOnly);
                    }
                    else
                    {
                        pos = Rand.Vector(2000.0f, Rand.RandSync.ClientOnly);
                    } 
                }
                else
                {
                    pos = (Vector2)position;
                }


                var prefab = prefabs[Rand.Int(prefabs.Count, Rand.RandSync.ClientOnly)];

                int amount = Rand.Range(prefab.SwarmMin, prefab.SwarmMax, Rand.RandSync.ClientOnly);
                List<BackgroundCreature> swarmMembers = new List<BackgroundCreature>();

                for (int n = 0; n < amount; n++)
                {
                    var newSprite = new BackgroundCreature(prefab, pos);
                    activeSprites.Add(newSprite);
                    swarmMembers.Add(newSprite);
                }
                if (amount > 0)
                {
                    new Swarm(swarmMembers, prefab.SwarmRadius);
                }
            }
        }

        public void ClearSprites()
        {
            activeSprites.Clear();
        }

        public void Update(float deltaTime, Camera cam)
        {
            if (checkActiveTimer < 0.0f)
            {
                foreach (BackgroundCreature sprite in activeSprites)
                {
                    sprite.Enabled = Math.Abs(sprite.TransformedPosition.X - cam.WorldViewCenter.X) < cam.WorldView.Width &&
                                     Math.Abs(sprite.TransformedPosition.Y - cam.WorldViewCenter.Y) < cam.WorldView.Height;
                }

                checkActiveTimer = CheckActiveInterval;
            }
            else
            {
                checkActiveTimer -= deltaTime;
            }

            foreach (BackgroundCreature sprite in activeSprites)
            {
                if (!sprite.Enabled) continue;
                sprite.Update(deltaTime);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (BackgroundCreature sprite in activeSprites)
            {
                if (!sprite.Enabled) continue;
                sprite.Draw(spriteBatch);
            }
        }
    }
}
