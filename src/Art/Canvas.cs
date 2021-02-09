﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Net.Sockets;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelArt {
    public class Canvas {

        public Vector2 pos, dimen;
        
        public Texture2D background;
        public int xPix, yPix;
        public int area;
        
        public List<Layer> layers = new List<Layer>();
        public Layer layer;
        public Color[] layerColor;

        public bool changesMade = false;

        public List<Undo> undos = new List<Undo>();

        public int selectedLayerIndex = 0;

        public bool hasPreview;
        public Texture2D previewTexture;
        public Color[] previewColor;
        
        
        
        // SPECIFICS
        
        // control-z
        public const float firstHoldZ = 0.3F, afterHoldZ = 0.075F;
        public float holdTimeZ;
        // rect tool
        public Point rectBegin;
        
        
        public Canvas(int xPix, int yPix) : this(Textures.genRect(Colors.erased, xPix, yPix)) {}

        public Canvas(Texture2D texture) {

            background = Textures.get("rect");
            
            layers.Add(new Layer(texture));
            xPix = texture.Width;
            yPix = texture.Height;
            area = xPix * yPix;
            
            pos = Vector2.Zero;
            dimen = new Vector2(1, (float) yPix / xPix) * 100;

            selectedLayerIndex = 0;
        }

        public void input(float deltaTime, KeyInfo keys, MouseInfo mouse) {

            layer = layers[selectedLayerIndex];
            layerColor = Util.colorArray(layer.texture); // very laggy for larger canvases
            Vector2 mousePos = toCanvas(mouse.pos);
            Vector2 lastMousePos = toCanvas(Main.lastMousePos());
            Point pixel = toPixel(mousePos);

            hasPreview = false;

            switch (Main.tool) {
                case Tool.Brush:

                    if (mouse.leftPressed) {
                        addUndo();
                        brushAt(mousePos);
                    }

                    if (mouse.leftDown) {
                        brushBetween(lastMousePos, mousePos);
                    }
                    break;
                
                case Tool.Eraser:

                    Color saveColor = Main.brushColor;
                    Main.brushColor = Colors.erased;
                    if (mouse.leftPressed) {
                        addUndo();
                        brushAt(mousePos);
                    }

                    if (mouse.leftDown) {
                        brushBetween(lastMousePos, mousePos);
                    }

                    Main.brushColor = saveColor;
                    break;
                
                case Tool.FillBucket:
                    if (mouse.leftPressed) {
                        addUndo();
                        fillAt(pixel);
                    }
                    break;
                
                case Tool.Rect:

                    if (mouse.leftPressed) {
                        rectBegin = pixel;
                        addUndo();
                    }

                    if (mouse.leftDown) {
                        
                        Point p1 = Util.max(Util.min(pixel, rectBegin), new Point(0, 0));
                        Point p2 = Util.min(Util.max(pixel, rectBegin), new Point(xPix - 1, yPix - 1));

                        hasPreview = true;
                        clearPreview();
                        iterBetween(p1, p2, (x, y) => setRGB(x, y, Main.brushColor, previewColor));
                    }

                    if (mouse.leftUnpressed) {
                        bindPreview();
                    }

                    break;
            }

            if (keys.down(Keys.Z) && keys.down(Keys.LeftControl)) {
                if (keys.pressed(Keys.Z)) {
                    holdTimeZ = firstHoldZ;
                    useUndo();
                }
                else {
                    holdTimeZ -= deltaTime;
                    if (holdTimeZ < 0) {
                        useUndo();
                        holdTimeZ = afterHoldZ;
                    }
                }
            }

            if (changesMade) {
                layer.texture.SetData(layerColor);
                changesMade = false;
            }

            previewTexture = hasPreview ? Textures.toTexture(previewColor, xPix, yPix) : null;
        }

        public void bindPreview() {
            GraphicsDevice g = Main.instance.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;
            RenderTarget2D renderTarget = new RenderTarget2D(
                g,
                xPix,
                yPix,
                false,
                g.PresentationParameters.BackBufferFormat,
                DepthFormat.Depth24);
            
            g.SetRenderTarget(renderTarget);

            g.Clear(Colors.erased);
            spriteBatch.Begin(SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            
            Rectangle rect = new Rectangle(0, 0, xPix, yPix);
            spriteBatch.Draw(layer.texture, rect, Color.White);
            spriteBatch.Draw(previewTexture, rect, Color.White);

            spriteBatch.End();
            g.SetRenderTarget(null);

            layer.texture = renderTarget;
        }

        public Texture2D genSingleImage() {
            GraphicsDevice g = Main.instance.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;
            RenderTarget2D renderTarget = new RenderTarget2D(
                g,
                xPix,
                yPix,
                false,
                g.PresentationParameters.BackBufferFormat,
                DepthFormat.Depth24);
            
            g.SetRenderTarget(renderTarget);

            g.Clear(Colors.erased);
            spriteBatch.Begin(SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            
            Rectangle rect = new Rectangle(0, 0, xPix, yPix);
            foreach (var layer in layers) {
                spriteBatch.Draw(layer.texture, rect, Color.White);
            }

            spriteBatch.End();
            g.SetRenderTarget(null);

            return renderTarget;
        }

        public void fillAt(Point pixel) {
            var full = Util.colorArray(genSingleImage());

            Color onColor = getRGB(pixel, full);
            
            var points = new List<Point> {pixel};

            setRGB(pixel, Main.brushColor);
            while (points.Count > 0) {
                Point point = points[^1];
                points.RemoveAt(points.Count - 1);

                Point test = new Point(point.X - 1, point.Y);
                if (test.X >= 0 && getRGB(test, full) == onColor && getRGB(test) != Main.brushColor) {
                    points.Add(test);
                    setRGB(test, Main.brushColor);
                }
                
                test = new Point(point.X + 1, point.Y);
                if (test.X < xPix && getRGB(test, full) == onColor && getRGB(test) != Main.brushColor) {
                    points.Add(test);
                    setRGB(test, Main.brushColor);
                }
                
                test = new Point(point.X, point.Y - 1);
                if (test.Y >= 0 && getRGB(test, full) == onColor && getRGB(test) != Main.brushColor) {
                    points.Add(test);
                    setRGB(test, Main.brushColor);
                }
                
                test = new Point(point.X, point.Y + 1);
                if (test.Y < yPix && getRGB(test, full) == onColor && getRGB(test) != Main.brushColor) {
                    points.Add(test);
                    setRGB(test, Main.brushColor);
                }
            }
        }

        public void clearPreview() {
            previewColor = new Color[area];
            for (int i = 0; i < previewColor.Length; i++) {
                previewColor[i] = Colors.erased;
            }
        }

        public void brushBetween(Vector2 start, Vector2 end) {
            
            Vector2 diff = end - start;
            float mag = Util.mag(diff);
            float angle = Util.angle(diff);
            
            for (float add = 0; add <= mag; add += 0.1F) {
                Vector2 canvasPos = Util.polar(add, angle) + start;
                brushAt(canvasPos);
            }
        }

        public void iterBetween(Point start, Point endInc, Action<int, int> action) { // end is inclusive
            for (int x = start.X; x <= endInc.X; x++) {
                for (int y = start.Y; y <= endInc.Y; y++) {
                    action.Invoke(x, y);
                }
            }
        }

        public void brushAt(Vector2 canvasPos) {
            Point pixel = toPixel(canvasPos);
            if (inBounds(pixel))
                setRGB(pixel, Main.brushColor);
        }

        public void render(Camera camera, SpriteBatch spriteBatch) {
            Rectangle renderRect = camera.toScreen(pos, dimen);
            
            spriteBatch.Draw(background, renderRect, Colors.canvasBack);

            foreach (var layer in layers) {
                spriteBatch.Draw(layer.texture, renderRect, Color.White);
                if (hasPreview && layer == this.layer) {
                    spriteBatch.Draw(previewTexture, renderRect, Color.White);
                }
            }
        }

        public void addUndo() {
            undos.Add(new Undo(this));
        }

        public void useUndo() {
            int index = undos.Count - 1;
            
            if (index < 0) return;
            
            undos[index].apply(this);
            undos.RemoveAt(index);

            layerColor = Util.colorArray(layer.texture);
        }

        public bool inBounds(int x, int y) {
            return (x >= 0 && x < xPix && y >= 0 && y < yPix);
        }

        public bool inBounds(Point pixel) {
            return inBounds(pixel.X, pixel.Y);
        }
        
        public void setRGB(Point point, Color color) {
            layerColor[point.X + point.Y * xPix] = color;
            changesMade = true;
        }
        
        public void setRGB(Point point, Color color, Color[] arr) {
            arr[point.X + point.Y * xPix] = color;
            changesMade = true;
        }
        
        public void setRGB(int x, int y, Color color, Color[] arr) {
            arr[x + y * xPix] = color;
            changesMade = true;
        }
        
        public void setRGB(int x, int y, Color color) {
            layerColor[x + y * xPix] = color;
            changesMade = true;
        }
        
        public Color getRGB(Point point) {
            return layerColor[point.X + point.Y * xPix];
        }
        
        public Color getRGB(int x, int y) {
            return layerColor[x + y * xPix];
        }
        
        public Color getRGB(Point point, Color[] arr) {
            return arr[point.X + point.Y * xPix];
        }
        
        public Color getRGB(int x, int y, Color[] arr) {
            return arr[x + y * xPix];
        }

        public Vector2 toCanvas(Vector2 screenPos) {
            return (Main.camera.toWorld(screenPos) - (pos - dimen/2)) / dimen * new Vector2(xPix, yPix);
        }

        public Point toPixel(Vector2 onCanvas) {
            return new Point((int)Math.Floor(onCanvas.X),(int)Math.Floor(onCanvas.Y));
        }
    }
}