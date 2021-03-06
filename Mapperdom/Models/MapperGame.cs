﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using System.Drawing;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas;
using Windows.Storage.Streams;
using Microsoft.Graphics.Canvas.Text;

namespace Mapperdom.Models
{
    public class MapperGame
    {
        public readonly WriteableBitmap baseImage;

        public PixelData[,] Pixels;

        public bool IsTreatyMode = false;

        public Dictionary<byte, Nation> Nations;

        public Dictionary<byte, WarSide> Sides;

        public string DialogText;
        public Rectangle DialogRectangle;

        private LinkedList<MapState> PreviousStates = new LinkedList<MapState>();
        private byte PreviousStatesPosition = 0;

        public string seed;
        private readonly Random rng;

        public Dictionary<UnorderedBytePair, sbyte> Fronts = new Dictionary<UnorderedBytePair, sbyte>();

        public MapperGame(WriteableBitmap map, int? seed = null, bool useCustomColors = false)
        {
            rng = seed != null ? new Random(seed.Value) : new Random();

            baseImage = map;

            byte[] imageArray = new byte[baseImage.PixelHeight * baseImage.PixelWidth * 4];

            using (Stream stream = baseImage.PixelBuffer.AsStream())
            {
                stream.Read(imageArray, 0, imageArray.Length);
            }

            Pixels = new PixelData[baseImage.PixelWidth, baseImage.PixelHeight];

            Sides = new Dictionary<byte, WarSide>();
            Nations = new Dictionary<byte, Nation>();

            DialogText = "";
            Rectangle newDialogRectangle = new Rectangle(0, 0, 0, 0);

            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {
                    Pixels[x, y].IsGained = false;
                    //Ocean
                    if (imageArray[4 * (y * baseImage.PixelWidth + x)] == 0 //Blue
                        && imageArray[4 * (y * baseImage.PixelWidth + x) + 1] == 0 //Green
                        && imageArray[4 * (y * baseImage.PixelWidth + x) + 2] == 0) //Red
                    {
                        Pixels[x, y].IsOcean = true;
                    }
                    //Dialog box
                    else if (imageArray[4 * (y * baseImage.PixelWidth + x)] == 255 //Blue
                        && imageArray[4 * (y * baseImage.PixelWidth + x) + 1] == 255 //Green
                        && imageArray[4 * (y * baseImage.PixelWidth + x) + 2] == 255) //Red
                    {
                        if (newDialogRectangle.Width == 0 || newDialogRectangle.Height == 0)
                        {
                            newDialogRectangle = new Rectangle(x, y, 1, 1);
                        }
                        else
                        {
                            if(newDialogRectangle.X > x)
                            {
                                newDialogRectangle.X = x;
                            }
                            else if(newDialogRectangle.X + newDialogRectangle.Width < x)
                            {
                                newDialogRectangle.Width = x - newDialogRectangle.X;
                            }

                            if (newDialogRectangle.Y > y)
                            {
                                newDialogRectangle.Y = y;
                            }
                            else if (newDialogRectangle.Y + newDialogRectangle.Height < y)
                            {
                                newDialogRectangle.Height = y - newDialogRectangle.Y;
                            }
                        }
                        Pixels[x, y].IsOcean = true;
                    }
                    //Land
                    else
                    {
                        System.Drawing.Color c = System.Drawing.Color.FromArgb(imageArray[4 * (y * baseImage.PixelWidth + x) + 2],
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1],
                            imageArray[4 * (y * baseImage.PixelWidth + x)]);

                        Nation foundNation = Nations.Values.FirstOrDefault(n => n.MainColor == c);

                        if(foundNation != null)
                        {
                            byte id = Nations.First(kvp => kvp.Value == foundNation).Key;
                            Pixels[x, y].IsOcean = false;
                            Pixels[x, y].OwnerId = id;
                            Pixels[x, y].OccupierId = id;
                        }
                        else
                        {
                            byte id = (byte)Nations.Count;
                            Nations.Add(id, new Nation("Nation " + id, c));
                            Pixels[x, y].IsOcean = false;
                            Pixels[x, y].OwnerId = id;
                            Pixels[x, y].OccupierId = id;
                        }
                    }
                }
            }

            DialogRectangle = newDialogRectangle;

            if(!useCustomColors)
            {
                foreach (Nation n in Nations.Values)
                    n.MainColor = System.Drawing.Color.FromArgb(int.Parse("31b305", System.Globalization.NumberStyles.HexNumber));
            }

        }


        public async Task<WriteableBitmap> GetCurrentMapAsync()
        {
            GenerateNationLabels();
            //Read base image
            WriteableBitmap newImage = new WriteableBitmap(baseImage.PixelWidth, baseImage.PixelHeight);
            byte[] imageArray = new byte[newImage.PixelHeight * newImage.PixelWidth * 4];

            using (Stream stream = baseImage.PixelBuffer.AsStream())
            {
                stream.Read(imageArray, 0, imageArray.Length);
            }

            GetPixelOverlay(ref imageArray);

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasBitmap newNewImage;

            newNewImage = CanvasBitmap.CreateFromBytes(device, imageArray, newImage.PixelWidth, newImage.PixelHeight, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized);

            CanvasRenderTarget offscreen = new CanvasRenderTarget(
                device, (float)newNewImage.Bounds.Width, (float)newNewImage.Bounds.Height, 96);


            using (CanvasDrawingSession ds = offscreen.CreateDrawingSession())
            {
                ds.DrawImage(newNewImage, newNewImage.Bounds);

                foreach (Nation nation in Nations.Values)
                {
                    if (nation.LabelFontSize == 0) continue;
                    CanvasTextFormat format = new CanvasTextFormat() { FontFamily = "Georgia", FontSize = ds.ConvertPixelsToDips(nation.LabelFontSize) };

                    if (nation.Name == null || nation.Name == "") continue;
                    CanvasTextLayout textLayout = new CanvasTextLayout(ds, nation.Name, format, 0f, 0f) { WordWrapping = CanvasWordWrapping.NoWrap };
                    ds.DrawText(nation.Name, ds.ConvertPixelsToDips(nation.LabelPosX) - (float)(textLayout.DrawBounds.Width / 2), ds.ConvertPixelsToDips(nation.LabelPosY) - (float)(textLayout.DrawBounds.Height / 2), Colors.Black, format);
                }

                Rectangle? smallerDialogRectangle = null;

                if (DialogRectangle.Width > 20 && DialogRectangle.Height > 20)
                {
                    smallerDialogRectangle = new Rectangle(DialogRectangle.X + 10, DialogRectangle.Y + 10, DialogRectangle.Width - 10, DialogRectangle.Height - 10);
                }

                if(smallerDialogRectangle.HasValue && DialogText != null && DialogText != "")
                {
                    CanvasTextFormat format = new CanvasTextFormat() { FontFamily = "Georgia", FontSize = ds.ConvertPixelsToDips(48) };

                    CanvasTextLayout textLayout = new CanvasTextLayout(ds, DialogText, format, 0f, 0f) { WordWrapping = CanvasWordWrapping.NoWrap };
                    ds.DrawText(DialogText,
                        new Windows.Foundation.Rect(ds.ConvertPixelsToDips(smallerDialogRectangle.Value.X), ds.ConvertPixelsToDips(smallerDialogRectangle.Value.Y), ds.ConvertPixelsToDips(smallerDialogRectangle.Value.Width), ds.ConvertPixelsToDips(smallerDialogRectangle.Value.Height)),
                        Windows.UI.Colors.Black,
                        format);
                }
            }


            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                await offscreen.SaveAsync(stream, CanvasBitmapFileFormat.Png);
                stream.Seek(0);
                await newImage.SetSourceAsync(stream);
            }

            return newImage;
        }

        public void GetPixelOverlay(ref byte[] imageArray)
        {
            Queue<Point> borders = new Queue<Point>();



            Rectangle? smallerDialogRectangle = null;

            if(DialogRectangle.Width > 16 && DialogRectangle.Height > 16)
            {
                 smallerDialogRectangle = new Rectangle(DialogRectangle.X + 8, DialogRectangle.Y + 8, DialogRectangle.Width - 16, DialogRectangle.Height - 16);
            }

            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {
                    if (Pixels[x, y].IsOcean)
                    {
                        if(DialogRectangle.Contains(x, y))
                        {
                            if(smallerDialogRectangle.HasValue && smallerDialogRectangle.Value.Contains(x, y))
                            {
                                imageArray[4 * (y * baseImage.PixelWidth + x)] = 255; // Blue
                                imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = 255; // Green
                                imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = 255; // Red
                            }
                            else
                            {
                                imageArray[4 * (y * baseImage.PixelWidth + x)] = 0; // Blue
                                imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = 0; // Green
                                imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = 0; // Red
                            }
                            continue;
                        }

                        imageArray[4 * (y * baseImage.PixelWidth + x)] = 95; // Blue
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = 47; // Green
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = 27; // Red
                        continue;
                    }
                    Nation officialNation = Nations[Pixels[x, y].OwnerId];
                    Nation occupierNation = Nations[Pixels[x, y].OccupierId];


                    if(IsTreatyMode && officialNation.IsSelected)
                    {
                        imageArray[4 * (y * baseImage.PixelWidth + x)] = 255; // Blue
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = 255; // Green
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = 255; // Red

                        continue;
                    }

                    if (!officialNation.WarSide.HasValue || !occupierNation.WarSide.HasValue)
                    {
                        if(officialNation.IsSelected)
                        {
                            imageArray[4 * (y * baseImage.PixelWidth + x)] = 255; // Blue
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = 255; // Green
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = 255; // Red
                        }
                        else
                        {
                            imageArray[4 * (y * baseImage.PixelWidth + x)] = officialNation.MainColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = officialNation.MainColor.G; // Green
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = officialNation.MainColor.R; // Red
                        }
                        continue;
                    }
                    WarSide officialSide = Sides[officialNation.WarSide.Value];
                    WarSide occupierSide = Sides[occupierNation.WarSide.Value];

                    if (Pixels[x, y].IsGained) //Was this the most recent capture?
                    {
                        imageArray[4 * (y * baseImage.PixelWidth + x)] = occupierSide.GainColor.B; // Blue
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = occupierSide.GainColor.G; // Green
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = occupierSide.GainColor.R; // Red
                        //imageArray[4 * (y * baseImage.PixelWidth + x) + 3] = 255; // Opacity
                    }
                    else if (officialNation.Master == null && officialSide == occupierSide) //Nation controls its own territory and is not puppetted
                    {
                        if (officialNation.IsSelected && !officialNation.IsSurrendered)
                        {
                            imageArray[4 * (y * baseImage.PixelWidth + x)] = 255; // Blue
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = 255; // Green
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = 255; // Red
                        }
                        else if(officialNation.IsSurrendered)
                        {
                            imageArray[4 * (y * baseImage.PixelWidth + x)] = occupierSide.OccupiedColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = occupierSide.OccupiedColor.G; // Green
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = occupierSide.OccupiedColor.R; // Red
                        }
                        else
                        {
                            imageArray[4 * (y * baseImage.PixelWidth + x)] = officialSide.MainColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = officialSide.MainColor.G; // Green
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = officialSide.MainColor.R; // Red
                            //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 255; // Opacity
                        }
                    }
                    else if (officialNation.Master == occupierNation || officialNation == occupierNation
                    ) //Nation controls a puppet's territory
                    {
                        imageArray[4 * (y * baseImage.PixelWidth + x)] = occupierSide.PuppetColor.B; // Blue
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = occupierSide.PuppetColor.G; // Green
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = occupierSide.PuppetColor.R; // Red
                        //imageArray[4 * (y * baseImage.PixelWidth + x) + 3] = 255; // Opacity
                    }
                    else //Nation is occupied by another nation
                    {
                        if (occupierNation.IsSelected)
                        {
                            imageArray[4 * (y * baseImage.PixelWidth + x)] = 191; // Blue
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = 191; // Green
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = 191; // Red
                        }
                        else
                        {
                            imageArray[4 * (y * baseImage.PixelWidth + x)] = occupierSide.OccupiedColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1] =
                                occupierSide.OccupiedColor.G; // Green
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2] =
                                occupierSide.OccupiedColor.R; // Red
                            //imageArray[4 * (y * baseImage.PixelWidth + x) + 3] = 255; // Opacity
                        }
                    }
                }
            }

            GetBorders(ref borders);
            while (borders.Count > 0)
            {
                Point p = borders.Dequeue();

                imageArray[4 * (p.Y * baseImage.PixelWidth + p.X)] = 0; // Blue
                imageArray[4 * (p.Y * baseImage.PixelWidth + p.X) + 1] = 0; // Green
                imageArray[4 * (p.Y * baseImage.PixelWidth + p.X) + 2] = 0; // Red
                //imageArray[4 * (y * baseImage.PixelWidth + x) + 3] = 255; // Opacity
            }
        }

        public void Advance()
        {
            ClearGains(); //Clears the resources from the last turn

            if(IsTreatyMode)
            {

            }
            else
            {
                //foreach (KeyValuePair<byte, Nation> n in Nations) //Key is Nation ID, Value is the Nation object
                //{
                //    //Nation.plan contains all the info passes to the "Expand" function when the turn is taken
                //    Expand(n.Key, n.Value.plan);
                //}

                foreach(UnorderedBytePair ids in Fronts.Keys)
                {
                    GainTerritory(ids);
                }
            }

            CheckForCollapse(); //Did a nation collapse?
        }

        public void BeginNavalInvasion(byte invader, byte defender)
        {
            Rectangle invaderBoundingBox = new Rectangle(0, 0, 0, 0);

            Queue<Point> defenderCoast = new Queue<Point>();

            GetCoastalPixels(defender, ref defenderCoast);

            if (defenderCoast.Count == 0) return;

            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (Pixels[x, y].IsOcean || Pixels[x, y].OccupierId != defender) continue;

                    if (invaderBoundingBox.Width == 0 && invaderBoundingBox.Height == 0)
                    {
                        invaderBoundingBox = new Rectangle(x, y, 1, 1);
                    }
                    else
                    {
                        Rectangle newRect = invaderBoundingBox;
                        bool hasChanged = false;

                        if (x < newRect.Left)
                        {
                            newRect.X = x;
                            hasChanged = true;
                        }
                        else if (x >= newRect.Right)
                        {
                            newRect.Width = x - newRect.X;
                            hasChanged = true;
                        }

                        if (y < newRect.Top)
                        {
                            newRect.Y = y;
                            hasChanged = true;
                        }
                        else if (y >= newRect.Bottom)
                        {
                            newRect.Height = y - newRect.Y;
                            hasChanged = true;
                        }

                        if (hasChanged)
                        {
                            invaderBoundingBox = newRect;
                        }
                    }
                }
            }

            if (invaderBoundingBox.Height == 0 || invaderBoundingBox.Width == 0) return;

            Point center = new Point(invaderBoundingBox.X + invaderBoundingBox.Width / 2, invaderBoundingBox.Y + invaderBoundingBox.Height / 2);
            defenderCoast.OrderBy(p => Math.Sqrt((center.X - p.X) * (center.X - p.X) + (center.Y - p.Y) * (center.Y - p.Y)));

            if(defenderCoast.Count < 25)
            {
                foreach(Point p in defenderCoast)
                {
                    Pixels[p.X, p.Y].OccupierId = invader;
                    Pixels[p.X, p.Y].IsGained = true;
                }
            }
            else
            {
                List<Point> nearbyCoast = defenderCoast.ToList().GetRange(0, defenderCoast.Count / 4);

                int numBeachheads = nearbyCoast.Count / 3 <= 15 ? 15 : nearbyCoast.Count / 3;

                do
                {
                    int i = rng.Next(0, nearbyCoast.Count - 1);
                    Point p = nearbyCoast[i];
                    nearbyCoast.RemoveAt(i);

                    Pixels[p.X, p.Y].OccupierId = invader;
                    Pixels[p.X, p.Y].IsGained = true;

                    numBeachheads--;
                }
                while (numBeachheads > 0);
            }

        }




        public void GainTerritory(UnorderedBytePair nationIds)
        {
            //Nation is not at war. Cannot expand this way
            if (!Nations[nationIds.GetSmallerByte()].WarSide.HasValue || !Nations[nationIds.GetLargerByte()].WarSide.HasValue)
                return;

            byte range = (byte)Math.Abs(Fronts[nationIds]);
            byte attacker;
            byte defender;

            if (Fronts[nationIds] > 0)
            {
                attacker = nationIds.GetLargerByte();
                defender = nationIds.GetSmallerByte();
            }
            else
            {
                attacker = nationIds.GetSmallerByte();
                defender = nationIds.GetLargerByte();
            }



            Queue<Point> outline = new Queue<Point>();

            GetCoastalPixels(attacker, ref outline);

            while(outline.Count > 0)
            {
                Point p = outline.Dequeue();
                if ((byte)rng.Next(0, 255) != 0) continue;


                byte direction = (byte)rng.Next(0, 255);

                if ((direction & (1 << 0)) != 0  && p.X > 0 && Pixels[p.X - 1, p.Y].IsOcean)
                {
                    AttemptRangedNavalLanding(nationIds, new Point(p.X - 1, p.Y),  -1, 0);
                }
                if ((direction & (1 << 0)) == 0 && p.X < Pixels.GetLength(0) - 1 && Pixels[p.X + 1, p.Y].IsOcean)
                {
                    AttemptRangedNavalLanding(nationIds, new Point(p.X + 1, p.Y), 1, 0);
                }
                if ((direction & (1 << 1)) != 0 && p.Y > 0 && Pixels[p.X, p.Y - 1].IsOcean)
                {
                    AttemptRangedNavalLanding(nationIds, new Point(p.X, p.Y - 1), 0, -1);
                }
                if ((direction & (1 << 1)) == 0 && p.Y < Pixels.GetLength(1) - 1 && Pixels[p.X, p.Y + 1].IsOcean)
                {
                    AttemptRangedNavalLanding(nationIds, new Point(p.X, p.Y + 1), 0, 1);
                }
            }


            Queue<Point> bounds = new Queue<Point>();
            Queue<Point> nextBounds = new Queue<Point>();

            GetFront(attacker, defender, ref bounds);

            if (Fronts[nationIds] == 0) return;

            while (range > 0)
            {
                while (bounds.Count > 0)
                {
                    Point p = bounds.Dequeue();

                    if (Pixels[p.X, p.Y].IsOcean) continue;
                    byte friendlyNumber = 0; //The number of friendly Pixels surrounding it (sea, nation, or allied)

                    if (p.X > 0)
                    {
                        if (Pixels[p.X - 1, p.Y].OccupierId == defender && !Pixels[p.X - 1, p.Y].IsOcean &&
                            (byte)rng.Next(0, 255) % 8 == 0)
                        {
                            Pixels[p.X - 1, p.Y].OccupierId = attacker;
                            nextBounds.Enqueue(new Point(p.X - 1, p.Y));
                            Pixels[p.X - 1, p.Y].IsGained = true;
                            friendlyNumber++;
                        }
                        else if (Pixels[p.X - 1, p.Y].IsOcean || Pixels[p.X - 1, p.Y].OccupierId == attacker)
                            friendlyNumber++;

                    }
                    else friendlyNumber++;

                    if (p.X < Pixels.GetLength(0) - 1)
                    {
                        if (Pixels[p.X + 1, p.Y].OccupierId == defender && !Pixels[p.X + 1, p.Y].IsOcean &&
                            (byte)rng.Next(0, 255) % 8 == 0)
                        {
                            Pixels[p.X + 1, p.Y].OccupierId = attacker;
                            nextBounds.Enqueue(new Point(p.X + 1, p.Y));
                            Pixels[p.X + 1, p.Y].IsGained = true;
                            friendlyNumber++;
                        }
                        else if (Pixels[p.X + 1, p.Y].IsOcean || Pixels[p.X + 1, p.Y].OccupierId == attacker)
                            friendlyNumber++;
                    }
                    else friendlyNumber++;

                    if (p.Y > 0)
                    {
                        if (Pixels[p.X, p.Y - 1].OccupierId == defender && !Pixels[p.X, p.Y - 1].IsOcean &&
                            (byte)rng.Next(0, 255) % 8 == 0)
                        {
                            Pixels[p.X, p.Y - 1].OccupierId = attacker;
                            nextBounds.Enqueue(new Point(p.X, p.Y - 1));
                            Pixels[p.X, p.Y - 1].IsGained = true;
                            friendlyNumber++;
                        }
                        else if (Pixels[p.X, p.Y - 1].IsOcean || Pixels[p.X, p.Y - 1].OccupierId == attacker)
                            friendlyNumber++;
                    }
                    else friendlyNumber++;

                    if (p.Y < Pixels.GetLength(1) - 1)
                    {
                        if (Pixels[p.X, p.Y + 1].OccupierId == defender && !Pixels[p.X, p.Y + 1].IsOcean &&
                            (byte)rng.Next(0, 255) % 8 == 0)
                        {
                            Pixels[p.X, p.Y + 1].OccupierId = attacker;
                            nextBounds.Enqueue(new Point(p.X, p.Y + 1));
                            Pixels[p.X, p.Y + 1].IsGained = true;
                            friendlyNumber++;
                        }
                        else if (Pixels[p.X, p.Y + 1].IsOcean || Pixels[p.X, p.Y + 1].OccupierId == attacker)
                            friendlyNumber++;
                    }
                    else friendlyNumber++;

                    //If not all Pixels are friendly, save this pixel for the next expansion
                    if (friendlyNumber < 4) nextBounds.Enqueue(p);
                }

                range--;
                bounds = nextBounds;
                nextBounds = new Queue<Point>();
            }
        }

        public void AnnexTerritory(byte nationId)
        {
            AnnexOccupation(nationId);
            CheckForCollapse();
        }

        public void Surrender(byte nationId)
        {
            ClearGains();
            //TODO: Make nations surrender to those they are warring with, not the first avialable one in the dictionary
            foreach (KeyValuePair<byte, Nation> kvp in Nations)
            {
                if (!kvp.Value.WarSide.HasValue || kvp.Value.WarSide == Nations[nationId].WarSide) continue;
                Surrender(nationId, kvp.Key, true);
                break;
            }
        }

        //Get Pixels with locations that border other nations (not including the sea)
        public void GetBorders(ref Queue<Point> borders)
        {
            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {
                    if (Pixels[x, y].IsOcean) continue;
                    if (x > 0 && !Pixels[x - 1, y].IsOcean && Pixels[x - 1, y].OwnerId != Pixels[x, y].OwnerId)
                    {
                        borders.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (x < Pixels.GetLength(0) - 1 && !Pixels[x + 1, y].IsOcean &&
                        Pixels[x + 1, y].OwnerId != Pixels[x, y].OwnerId)
                    {
                        borders.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y > 0 && !Pixels[x, y - 1].IsOcean && Pixels[x, y - 1].OwnerId != Pixels[x, y].OwnerId)
                    {
                        borders.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y < Pixels.GetLength(1) - 1 && !Pixels[x, y + 1].IsOcean &&
                        Pixels[x, y + 1].OwnerId != Pixels[x, y].OwnerId)
                    {
                        borders.Enqueue(new Point(x, y));
                        continue;
                    }
                }
            }
        }




        public void Surrender(byte destroyedNationId, byte destroyerNationId, bool realisticSurrender)
        {
            if (Nations[destroyedNationId].IsSurrendered)
                return;
            Nations[destroyedNationId].IsSurrendered = true;

            if(!realisticSurrender)
            {
                for (int y = 0; y < Pixels.GetLength(1); y++)
                {
                    for (int x = 0; x < Pixels.GetLength(0); x++)
                    {
                        if (Pixels[x, y].OccupierId != destroyedNationId) continue;
                        Pixels[x, y].OccupierId = destroyerNationId;
                        Pixels[x, y].IsGained = true;
                    }
                }
            }
            else
            {
                Queue<Point> bounds = new Queue<Point>();
                GetFrontalPixels(destroyedNationId, ref bounds);

                while(bounds.Count > 0)
                {
                    Point p = bounds.Dequeue();

                    if (!Pixels[p.X, p.Y].IsOcean && !Nations[Pixels[p.X, p.Y].OccupierId].WarSide.HasValue) continue;
                    if (Pixels[p.X, p.Y].IsOcean) continue;

                    if (p.X > 0 && Pixels[p.X - 1, p.Y].OccupierId == destroyedNationId)
                    {
                            if (!Pixels[p.X, p.Y].IsOcean && Nations[Pixels[p.X, p.Y].OccupierId].WarSide.HasValue && Nations[Pixels[p.X, p.Y].OccupierId].WarSide != Nations[destroyedNationId].WarSide)
                                Pixels[p.X - 1, p.Y].OccupierId = Pixels[p.X, p.Y].OccupierId;
                            else Pixels[p.X - 1, p.Y].OccupierId = destroyerNationId;

                            Pixels[p.X - 1, p.Y].IsGained = true;

                            bounds.Enqueue(new Point(p.X - 1, p.Y));
                    }

                    if (p.X < Pixels.GetLength(0) - 1 && Pixels[p.X + 1, p.Y].OccupierId == destroyedNationId)
                    {
                            if (!Pixels[p.X, p.Y].IsOcean && Nations[Pixels[p.X, p.Y].OccupierId].WarSide.HasValue && Nations[Pixels[p.X, p.Y].OccupierId].WarSide != Nations[destroyedNationId].WarSide)
                                Pixels[p.X + 1, p.Y].OccupierId = Pixels[p.X, p.Y].OccupierId;
                            else Pixels[p.X + 1, p.Y].OccupierId = destroyerNationId;

                            Pixels[p.X + 1, p.Y].IsGained = true;

                            bounds.Enqueue(new Point(p.X + 1, p.Y));
                    }

                    if (p.Y > 0 && Pixels[p.X, p.Y - 1].OccupierId == destroyedNationId)
                    {
                            if (!Pixels[p.X, p.Y].IsOcean && Nations[Pixels[p.X, p.Y].OccupierId].WarSide.HasValue && Nations[Pixels[p.X, p.Y].OccupierId].WarSide != Nations[destroyedNationId].WarSide)
                                Pixels[p.X, p.Y - 1].OccupierId = Pixels[p.X, p.Y].OccupierId;
                            else Pixels[p.X, p.Y - 1].OccupierId = destroyerNationId;

                            Pixels[p.X, p.Y - 1].IsGained = true;

                            bounds.Enqueue(new Point(p.X, p.Y - 1));
                    }

                    if (p.Y < Pixels.GetLength(1) - 1 && Pixels[p.X, p.Y + 1].OccupierId == destroyedNationId)
                    {
                            if (!Pixels[p.X, p.Y].IsOcean && Nations[Pixels[p.X, p.Y].OccupierId].WarSide.HasValue && Nations[Pixels[p.X, p.Y].OccupierId].WarSide != Nations[destroyedNationId].WarSide)
                                Pixels[p.X, p.Y + 1].OccupierId = Pixels[p.X, p.Y].OccupierId;
                            else Pixels[p.X, p.Y + 1].OccupierId = destroyerNationId;

                            Pixels[p.X, p.Y + 1].IsGained = true;

                            bounds.Enqueue(new Point(p.X, p.Y + 1));
                    }
                }

                for (int y = 0; y < Pixels.GetLength(1); y++)
                {
                    for (int x = 0; x < Pixels.GetLength(0); x++)
                    {
                        if (Pixels[x, y].OccupierId != destroyedNationId) continue;
                        Pixels[x, y].OccupierId = destroyerNationId;
                        Pixels[x, y].IsGained = true;
                    }
                }


            }
        }

        public void CheckForCollapse()
        {
            List<byte> nationsToKeep = new List<byte>();
            List<byte> nationsToRemove = new List<byte>();

            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (!Pixels[x, y].IsOcean && !nationsToKeep.Contains(Pixels[x, y].OwnerId))
                        nationsToKeep.Add(Pixels[x, y].OwnerId);
                    if (!Pixels[x, y].IsOcean && !nationsToKeep.Contains(Pixels[x, y].OccupierId))
                        nationsToKeep.Add(Pixels[x, y].OccupierId);
                }
            }

            foreach (KeyValuePair<byte, Nation> pair in Nations)
            {
                if (!nationsToKeep.Contains(pair.Key)) nationsToRemove.Add(pair.Key);
            }

            foreach (byte id in nationsToRemove)
            {
                if (Nations[id].WarSide.HasValue)
                {
                    byte currentSide = Nations[id].WarSide.Value;
                    Nations.Remove(id);
                    Collapse(id,
                        Nations.First(pair => pair.Value.WarSide != currentSide)
                            .Key); //TODO: Make nation collapse to nation with majority of territory conquered
                }
                else
                {
                    //Nation is not at war and has no territory (shouldn't happen naturally unless borders are changed)
                    Nations.Remove(id);
                }
            }

            List<byte> sidesToRemove = new List<byte>();
            foreach (KeyValuePair<byte, WarSide> pair in Sides)
            {
                if (Nations.Values.Count(n => n.WarSide == pair.Key) == 0)
                    sidesToRemove.Add(pair.Key);
            }

            foreach (byte id in sidesToRemove)
            {
                Sides.Remove(id);
            }
            CleanFronts();
        }

        public void Collapse(byte destroyedNationId, byte destroyerNationId)
        {
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (Pixels[x, y].OccupierId == destroyedNationId)
                    {
                        Pixels[x, y].OccupierId = destroyedNationId;
                        Pixels[x, y].IsGained = true;
                    }

                    if (Pixels[x, y].OwnerId == destroyedNationId)
                        Pixels[x, y].OwnerId = destroyerNationId;
                }
            }
        }

        private void ClearGains()
        {
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    Pixels[x, y].IsGained = false;
                }
            }
        }


        private void GetFrontalPixels(byte owner, ref Queue<Point> bounds)
        {
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (!Pixels[x, y].IsOcean && Pixels[x, y].OccupierId == owner) continue;

                    if (x > 0 && !Pixels[x - 1, y].IsOcean && Pixels[x - 1, y].OccupierId == owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (x < Pixels.GetLength(0) - 1 && !Pixels[x + 1, y].IsOcean && Pixels[x + 1, y].OccupierId == owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y > 0 && !Pixels[x, y - 1].IsOcean && Pixels[x, y - 1].OccupierId == owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y < Pixels.GetLength(1) - 1 && !Pixels[x, y + 1].IsOcean && Pixels[x, y + 1].OccupierId == owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }
                }
            }
        }

        private void GetCoastalPixels(byte owner, ref Queue<Point> bounds)
        {
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (Pixels[x, y].IsOcean) continue;
                    if (Pixels[x, y].OccupierId != owner) continue;

                    if (x > 0 && Pixels[x - 1, y].IsOcean)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (x < Pixels.GetLength(0) - 1 && Pixels[x + 1, y].IsOcean)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y > 0 && Pixels[x, y - 1].IsOcean)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y < Pixels.GetLength(1) - 1 && Pixels[x, y + 1].IsOcean)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }
                }
            }
        }

        private void GetFront(byte nation1, byte nation2, ref Queue<Point> bounds)
        {
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (Pixels[x, y].IsOcean || Pixels[x, y].OccupierId != nation1) continue;
                    if (x > 0 && !Pixels[x - 1, y].IsOcean && Pixels[x - 1, y].OccupierId == nation2)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (x < Pixels.GetLength(0) - 1 && !Pixels[x + 1, y].IsOcean && Pixels[x + 1, y].OccupierId == nation2)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y > 0 && !Pixels[x, y - 1].IsOcean && Pixels[x, y - 1].OccupierId == nation2)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y < Pixels.GetLength(1) - 1 && !Pixels[x, y + 1].IsOcean && Pixels[x, y + 1].OccupierId == nation2)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }
                }
            }
        }


        private void GetBoundaryPixels(byte owner, ref Queue<Point> bounds)
        {
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (Pixels[x, y].IsOcean || Pixels[x, y].OccupierId != owner) continue;
                    if (x > 0 && !Pixels[x - 1, y].IsOcean && Pixels[x - 1, y].OccupierId != owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (x < Pixels.GetLength(0) - 1 && !Pixels[x + 1, y].IsOcean && Pixels[x + 1, y].OccupierId != owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y > 0 && !Pixels[x, y - 1].IsOcean && Pixels[x, y - 1].OccupierId != owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y < Pixels.GetLength(0) - 1 && !Pixels[x, y + 1].IsOcean && Pixels[x, y + 1].OccupierId != owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }
                }
            }
        }

        private void AttemptRangedNavalLanding(UnorderedBytePair nationIds, Point p, sbyte xDir, sbyte yDir)
        {
            if (xDir > 1) xDir = 1;
            else if (xDir < -1) xDir = -1;
            if (yDir > 1) yDir = 1;
            else if (yDir < -1) yDir = -1;

            byte range = (byte)Math.Abs(Fronts[nationIds]);
            byte attacker;
            byte defender;

            if (Fronts[nationIds] > 0)
            {
                attacker = nationIds.GetLargerByte();
                defender = nationIds.GetSmallerByte();
            }
            else
            {
                attacker = nationIds.GetSmallerByte();
                defender = nationIds.GetLargerByte();
            }


            while (p.X > 0 && p.Y > 0 && p.X < Pixels.GetLength(0) - 1 && p.Y < Pixels.GetLength(1) - 1 && range > 0)
            {
                p.X += xDir;
                p.Y += yDir;
                range--;

                if (Pixels[p.X, p.Y].IsOcean) continue;
                if(Pixels[p.X, p.Y].OccupierId == defender)
                    Pixels[p.X, p.Y].OccupierId = attacker;
                break;
            }
        }





        private void AttemptNavalLanding(byte conqueror, Point p, sbyte xDir, sbyte yDir)
        {
            if (xDir > 1) xDir = 1;
            else if (xDir < -1) xDir = -1;
            if (yDir > 1) yDir = 1;
            else if (yDir < -1) yDir = -1;

            while (p.X > 0 && p.Y > 0 && p.X < Pixels.GetLength(0) - 1 && p.Y < Pixels.GetLength(1) - 1)
            {
                p.X += xDir;
                p.Y += yDir;

                if (Pixels[p.X, p.Y].IsOcean) continue;
                if (Nations[Pixels[p.X, p.Y].OccupierId].WarSide.HasValue &&
                    Nations[Pixels[p.X, p.Y].OccupierId].WarSide != Nations[conqueror].WarSide)
                    Pixels[p.X, p.Y].OccupierId = conqueror;
                break;
            }
        }

        public void AnnexOccupation(byte ownerId)
        {
            /*
            bool[,] bounds = GetBoundaryPixels(ref occupationData, ownerId);
            uint xVal;
            uint yVal;

            do
            {
                xVal = (uint)rng.Next(0, bounds.GetLength(0));
                yVal = (uint)rng.Next(0, bounds.GetLength(1));
            }
            while (!(occupationData[xVal, yVal] == ownerId && !bounds[xVal, yVal]));

            FillAroundPixel(ref bounds, xVal, yVal);
            */


            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    //if (bounds[x, y])
                    if (Pixels[x, y].OccupierId == ownerId)
                        Pixels[x, y].OwnerId = ownerId;
                }
            }
            GenerateNationLabels();
        }

        //Recursively fill in surrounding Pixels with data until it hits a border of some kind
        private void FillAroundPixel(ref bool[,] bounds, uint xVal, uint yVal)
        {
            if (bounds[xVal, yVal]) return;
            bounds[xVal, yVal] = true;
            if (xVal > 0)
                FillAroundPixel(ref bounds, xVal - 1, yVal);
            if (xVal < bounds.GetLength(0))
                FillAroundPixel(ref bounds, xVal + 1, yVal);

            if (yVal > 0)
                FillAroundPixel(ref bounds, xVal, yVal - 1);
            if (yVal < bounds.GetLength(1))
                FillAroundPixel(ref bounds, xVal, yVal + 1);
        }

        public void Backup()
        {
            for(int i = PreviousStates.Count; i > PreviousStatesPosition; i--)
            {
                PreviousStates.RemoveLast();
            }

            if(PreviousStates.Count >= 10)
            {
                PreviousStates.RemoveFirst();
            }

            Dictionary<byte, Nation> clonedNations = new Dictionary<byte, Nation>();
            foreach(KeyValuePair<byte, Nation> pair in Nations)
            {
                clonedNations.Add(pair.Key, (Nation)pair.Value.Clone());
            }
            Dictionary<byte, WarSide> clonedSides = new Dictionary<byte, WarSide>();
            foreach (KeyValuePair<byte, WarSide> pair in Sides)
            {
                clonedSides.Add(pair.Key, (WarSide)pair.Value.Clone());
            }



            PreviousStates.AddLast(new MapState()
            {
                Pixels = (PixelData[,])Pixels.Clone(),
                Nations = clonedNations,
                Sides = clonedSides,
                Fronts = new Dictionary<UnorderedBytePair, sbyte>(Fronts),
                DialogText = DialogText,
                IsTreatyMode = IsTreatyMode
            });
            PreviousStatesPosition = (byte)(PreviousStates.Count);
        }

        public void Undo()
        {
            if (PreviousStatesPosition == 0)
                return;

            MapState obtainedState = PreviousStates.ElementAt(PreviousStatesPosition - 2);

            Nations = obtainedState.Nations;
            Sides = obtainedState.Sides;
            Pixels = obtainedState.Pixels;
            Fronts = obtainedState.Fronts;
            DialogText = obtainedState.DialogText;
            IsTreatyMode = obtainedState.IsTreatyMode;
            PreviousStatesPosition--;
        }

        public void Redo()
        {
            if (PreviousStatesPosition == PreviousStates.Count)
                return;
            MapState obtainedState = PreviousStates.ElementAt(PreviousStatesPosition);

            Nations = obtainedState.Nations;
            Sides = obtainedState.Sides;
            Pixels = obtainedState.Pixels;
            Fronts = obtainedState.Fronts;

            DialogText = obtainedState.DialogText;
            IsTreatyMode = obtainedState.IsTreatyMode;
            PreviousStatesPosition++;
        }

        public void DeclareWar(byte nation1Id, byte nation2Id, WarSide nation1Side, WarSide nation2Side)
        {
            byte newWarSideId = 0;

            if(!Nations[nation1Id].WarSide.HasValue)
            {
                if (!Sides.ContainsValue(nation1Side))
                {
                    while (Sides.ContainsKey(newWarSideId)) newWarSideId++;

                    Sides.Add(newWarSideId, nation1Side);

                    Nations[nation1Id].WarSide = newWarSideId;
                }
                else Nations[nation1Id].WarSide = Sides.First(kvp => kvp.Value == nation1Side).Key;
            }
            if (!Nations[nation2Id].WarSide.HasValue)
            {
                if (!Sides.ContainsValue(nation2Side))
                {
                    while (Sides.ContainsKey(newWarSideId)) newWarSideId++;

                    Sides.Add(newWarSideId, nation2Side);

                    Nations[nation2Id].WarSide = newWarSideId;
                }
                else Nations[nation2Id].WarSide = Sides.First(kvp => kvp.Value == nation2Side).Key;
            }

            CleanFronts();
        }

        public void StartUprising(byte nationID, Nation rebels, WarSide nationSide, WarSide rebelSide)
        {
            uint numPixels = 0;
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (Pixels[x, y].OwnerId == nationID) numPixels++;
                }
            }

            if (numPixels < 128) //Not a large enough nation for a rebellion to start
                return;


            byte newNationId = 0;
            byte newWarSideId = 0;

            while (Nations.ContainsKey(newNationId)) newNationId++;
            Nations.Add(newNationId, rebels);

            if (!Sides.ContainsValue(rebelSide))
            {
                while (Sides.ContainsKey(newWarSideId)) newWarSideId++;

                if (!Sides.ContainsValue(rebelSide)) Sides.Add(newWarSideId, rebelSide);
                Nations[newNationId].WarSide = newWarSideId;
            }
            else
            {
                Nations[newNationId].WarSide = Sides.FirstOrDefault(s => s.Value == rebelSide).Key;
            }

            if (!Nations[nationID].WarSide.HasValue)
            {
                newWarSideId = 0;
                while (Sides.ContainsKey(newWarSideId)) newWarSideId++;
                if (!Sides.ContainsValue(nationSide)) Sides.Add(newWarSideId, nationSide);
                Nations[nationID].WarSide = newWarSideId;
            }

            for (byte b = 0; b < 31; b++)
            {
                int x1;
                int y1;
                do
                {
                    x1 = rng.Next(0, Pixels.GetLength(0) - 1);
                    y1 = rng.Next(0, Pixels.GetLength(1) - 1);
                } while (Pixels[x1, y1].IsOcean || Pixels[x1, y1].OwnerId != nationID);

                Pixels[x1, y1].OccupierId = newNationId;
            }
        }

        public void GenerateNationLabels()
        {
            Dictionary<byte, Rectangle> boundsDictionary = new Dictionary<byte, Rectangle>();

            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (Pixels[x, y].IsOcean) continue;

                    if(!boundsDictionary.ContainsKey(Pixels[x, y].OwnerId))
                    {
                        boundsDictionary.Add(Pixels[x, y].OwnerId, new Rectangle(x, y, 1, 1));
                    }
                    else
                    {
                        Rectangle newRect = boundsDictionary[Pixels[x, y].OwnerId];
                        bool hasChanged = false;

                        if (x < newRect.Left)
                        {
                            newRect.X = x;
                            hasChanged = true;
                        }
                        else if (x >= newRect.Right)
                        {
                            newRect.Width = x - newRect.X;
                            hasChanged = true;
                        }

                        if (y < newRect.Top)
                        {
                            newRect.Y = y;
                            hasChanged = true;
                        }
                        else if (y >= newRect.Bottom)
                        {
                            newRect.Height = y - newRect.Y;
                            hasChanged = true;
                        }

                        if (hasChanged)
                        {
                            boundsDictionary[Pixels[x, y].OwnerId] = newRect;
                        }
                    }
                }
            }

            foreach (KeyValuePair<byte, Rectangle> kvp in boundsDictionary)
            {
                List<Rectangle> rectangles = new List<Rectangle>();
                List<Rectangle> activeRectangles = new List<Rectangle>();
                for (int y = kvp.Value.Top; y <= kvp.Value.Bottom; y++)
                {
                    for(int i = activeRectangles.Count - 1; i >= 0; i--)
                    {
                        bool isRemoved = false;
                        for (int x = activeRectangles[i].Left; x < activeRectangles[i].Right; x++)
                        {
                            if (Pixels[x, y].IsOcean || Pixels[x, y].OwnerId != kvp.Key)
                            {
                                rectangles.Add(activeRectangles[i]);
                                activeRectangles.RemoveAt(i);
                                isRemoved = true;
                                break;
                            }
                        }
                        if(!isRemoved)
                            activeRectangles[i] = new Rectangle(activeRectangles[i].X, activeRectangles[i].Y, activeRectangles[i].Width, activeRectangles[i].Height + 1);
                    }

                    Rectangle? last = null;
                    for (int x = kvp.Value.Left; x < kvp.Value.Right; x++)
                    {
                        if (!Pixels[x, y].IsOcean && Pixels[x, y].OwnerId == kvp.Key)
                        {
                            if(((x > 0 && (Pixels[x - 1, y].IsOcean || Pixels[x - 1, y].OwnerId != kvp.Key))
                                && (y > 0 && (Pixels[x, y - 1].IsOcean || Pixels[x, y - 1].OwnerId != kvp.Key)))
                                ||
                                ((x == 0)
                                && (y > 0 && (Pixels[x, y - 1].IsOcean || Pixels[x, y - 1].OwnerId != kvp.Key)))
                                ||
                                ((x > 0 && (Pixels[x - 1, y].IsOcean || Pixels[x - 1, y].OwnerId != kvp.Key))
                                && (y== 0)))
                            {
                                last = new Rectangle(x, y, 1, 1);
                                activeRectangles.Add(last.Value);
                            }
                            else if(last.HasValue)
                            {
                                last = new Rectangle(last.Value.X, last.Value.Y, last.Value.Width + 1, last.Value.Height);
                                activeRectangles[activeRectangles.Count - 1] = last.Value;
                            }
                        }
                        else
                        {
                            last = null;
                        }
                    }
                }

                foreach(Rectangle rect in activeRectangles)
                {
                    rectangles.Add(rect);
                }


                rectangles.OrderBy(r => r.Width * r.Height);
                rectangles.Reverse();
                Rectangle? biggest = null;
                int biggestFontSize = 0;
                foreach(Rectangle rect in rectangles.Where(r => r.Width > r.Height))
                {
                    if (rect.Height < 8 || rect.Width < 8 * Nations[kvp.Key].Name.Length)
                        continue;

                    int fontSize = 0;
                    if (rect.Height < rect.Width / Nations[kvp.Key].Name.Length)
                        fontSize = rect.Height;
                    else fontSize = rect.Width / Nations[kvp.Key].Name.Length;

                    if (fontSize < biggestFontSize) continue;

                    biggestFontSize = fontSize;
                    biggest = rect;
                }

                if(biggest.HasValue)
                {
                    Nations[kvp.Key].LabelFontSize = biggestFontSize;

                    Nations[kvp.Key].LabelPosX = biggest.Value.X + biggest.Value.Width / 2;
                    Nations[kvp.Key].LabelPosY = biggest.Value.Y + biggest.Value.Height / 2;
                }
                else
                {
                    Nations[kvp.Key].LabelFontSize = 0;
                    Nations[kvp.Key].LabelPosX = rectangles[0].X + rectangles[0].Width / 2;
                    Nations[kvp.Key].LabelPosY = rectangles[0].Y + rectangles[0].Height / 2;
                }
            }
        }

        public void WithdrawFromWar(byte nationId)
        {
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (Pixels[x, y].IsOcean) continue;

                    if (Pixels[x, y].OwnerId == nationId && Pixels[x, y].OccupierId != nationId)
                    {
                        Pixels[x, y].IsGained = false;
                        Pixels[x, y].OccupierId = nationId;
                    }
                    if(Pixels[x, y].OccupierId == nationId && Pixels[x,y].OwnerId != nationId)
                    {
                        if (Nations[Pixels[x, y].OwnerId].IsSurrendered)
                        {
                            //TEMPORARY: If a nation is surrendered, un-surrender them, and give them back the territory
                            //TODO: Give the occupied territory to a neighbor of the same alliance. If none exists, give it to the enemy or split it down the middle

                            Nations[Pixels[x, y].OwnerId].IsSurrendered = false;
                            Pixels[x, y].IsGained = true;
                            Pixels[x, y].OccupierId = Pixels[x, y].OwnerId;
                        }
                        else
                        {
                            Pixels[x, y].IsGained = true;
                            Pixels[x, y].OccupierId = Pixels[x, y].OwnerId;
                        }
                    }
                }
            }


            List<KeyValuePair<byte, Nation>> allies = Nations.Where(kvp => kvp.Value.WarSide == Nations[nationId].WarSide).ToList();

            if(allies.Count == 1)
            {
                Sides.Remove(Nations[nationId].WarSide.Value);

                if(Sides.Count == 1)
                {
                    Sides.Clear();
                    foreach(Nation n in Nations.Values)
                    {
                        n.WarSide = null;
                    }
                }
            }
            Nations[nationId].WarSide = null;
        }

        public bool CanUndo => PreviousStatesPosition >= 0;
        public bool CanRedo => PreviousStatesPosition < PreviousStates.Count;
        public void CleanFronts()
        {
            foreach(UnorderedBytePair pair in Fronts.Keys.ToList())
            {
                //If a nation was remove, a nation is not at war or nations wind up on the same side, throw out their values
                if(!(Nations.ContainsKey(pair.GetSmallerByte()) && Nations.ContainsKey(pair.GetLargerByte())) || !(Nations[pair.GetSmallerByte()].WarSide.HasValue || Nations[pair.GetLargerByte()].WarSide.HasValue) || Nations[pair.GetSmallerByte()].WarSide == Nations[pair.GetLargerByte()].WarSide)
                {
                    Fronts.Remove(pair);
                }
            }

            foreach(KeyValuePair<byte, Nation> p1 in Nations)
            {
                foreach (KeyValuePair<byte, Nation> p2 in Nations)
                {
                    if (p1.Equals(p2)) continue;
                    if (Fronts.ContainsKey(new UnorderedBytePair(p1.Key, p2.Key)) || Fronts.ContainsKey(new UnorderedBytePair(p2.Key, p1.Key))) continue;
                    if(p1.Value.WarSide != p2.Value.WarSide && p1.Value.WarSide.HasValue && p2.Value.WarSide.HasValue) Fronts.Add(new UnorderedBytePair(p1.Key, p2.Key), 0);
                }
            }
        }
    }
}
