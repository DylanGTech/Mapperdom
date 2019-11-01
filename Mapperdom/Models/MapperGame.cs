using System;
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

namespace Mapperdom.Models
{
    public class MapperGame
    {
        public readonly WriteableBitmap baseImage;

        public PixelData[,] Pixels;

        public bool IsTreatyMode = false;

        public Dictionary<byte, Nation> Nations;

        public Dictionary<byte, WarSide> Sides;

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
                    //Land
                    else
                    {
                        System.Drawing.Color c = System.Drawing.Color.FromArgb(imageArray[4 * (y * baseImage.PixelWidth + x)],
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1],
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2]);

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

            if(!useCustomColors)
            {
                foreach (Nation n in Nations.Values)
                    n.MainColor = System.Drawing.Color.FromArgb(int.Parse("31b305", System.Globalization.NumberStyles.HexNumber));
            }

        }


        public WriteableBitmap GetCurrentMap()
        {
            //Read base image
            WriteableBitmap newImage = new WriteableBitmap(baseImage.PixelWidth, baseImage.PixelHeight);
            byte[] imageArray = new byte[newImage.PixelHeight * newImage.PixelWidth * 4];

            using (Stream stream = baseImage.PixelBuffer.AsStream())
            {
                stream.Read(imageArray, 0, imageArray.Length);
            }

            GetPixelOverlay(ref imageArray);

            //Write pixel array to final image
            using (Stream stream = newImage.PixelBuffer.AsStream())
            {
                stream.Write(imageArray, 0, imageArray.Length);
            }

            return newImage;
        }

        public void GetPixelOverlay(ref byte[] imageArray)
        {
            Queue<Point> borders = new Queue<Point>();

            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {
                    if (Pixels[x, y].IsOcean)
                    {
                        imageArray[4 * (y * baseImage.PixelWidth + x)] = 126; // Blue
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = 56; // Green
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = 39; // Red
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
                        //imageArray[4 * (y * baseImage.PixelWidth + x) + 3] = 0; // Opacity
                    }
                    else if (officialNation.Master == null && officialSide == occupierSide) //Nation controls its own territory and is not puppetted
                    {
                        if (officialNation.IsSelected)
                        {
                            imageArray[4 * (y * baseImage.PixelWidth + x)] = 255; // Blue
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = 255; // Green
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = 255; // Red
                        }
                        else
                        {
                            imageArray[4 * (y * baseImage.PixelWidth + x)] = officialSide.MainColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = officialSide.MainColor.G; // Green
                            imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = officialSide.MainColor.R; // Red
                            //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                        }
                    }
                    else if (officialNation.Master == occupierNation || officialNation == occupierNation
                    ) //Nation controlls a puppet's territory
                    {
                        imageArray[4 * (y * baseImage.PixelWidth + x)] = occupierSide.PuppetColor.B; // Blue
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 1] = occupierSide.PuppetColor.G; // Green
                        imageArray[4 * (y * baseImage.PixelWidth + x) + 2] = occupierSide.PuppetColor.R; // Red
                        //imageArray[4 * (y * baseImage.PixelWidth + x) + 3] = 0; // Opacity
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
                            //imageArray[4 * (y * baseImage.PixelWidth + x) + 3] = 0; // Opacity
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
                //imageArray[4 * (y * baseImage.PixelWidth + x) + 3] = 0; // Opacity
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
                        if(!Pixels[p.X, p.Y].IsOcean && Nations[Pixels[p.X, p.Y].OccupierId].WarSide.HasValue && Nations[Pixels[p.X, p.Y].OccupierId].WarSide != Nations[destroyedNationId].WarSide)
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
                        if (Pixels[p.X, p.Y].IsGained && Nations[Pixels[p.X, p.Y].OccupierId].WarSide.HasValue && Nations[Pixels[p.X, p.Y].OccupierId].WarSide != Nations[destroyedNationId].WarSide)
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
                    //Nation is not at war and has no territory (shouldn't happen naturally)
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

                    if (y < Pixels.GetLength(0) - 1 && !Pixels[x, y + 1].IsOcean && Pixels[x, y + 1].OccupierId == nation2)
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



            PreviousStates.AddLast(new MapState((PixelData[,])Pixels.Clone(), clonedNations, clonedSides, new Dictionary<UnorderedBytePair, sbyte>(Fronts)));
            PreviousStatesPosition = (byte)(PreviousStates.Count);
        }

        public void Undo()
        {
            if (PreviousStatesPosition == 0)
                return;

            MapState obtainedState = PreviousStates.ElementAt(PreviousStatesPosition - 1);

            Nations = obtainedState.Nations;
            Sides = obtainedState.Sides;
            Pixels = obtainedState.Pixels;
            Fronts = obtainedState.Fronts;
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

        public void WithdrawFromWar(byte nationID, bool keepLandOccuppied = false)
        {
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                for (int x = 0; x < Pixels.GetLength(0); x++)
                {
                    if (Pixels[x, y].OwnerId == nationID || Pixels[x, y].OccupierId == nationID)
                    {
                        if(keepLandOccuppied)
                        {
                            Pixels[x, y].OwnerId = Pixels[x, y].OccupierId;
                        }
                        else
                        {
                            Pixels[x, y].OccupierId = Pixels[x, y].OwnerId;
                        }
                    }
                }
            }

            byte side = Nations[nationID].WarSide.Value;
            Nations[nationID].WarSide = null;


            if (Nations.Values.Where(n => n.WarSide == side).ToList().Count == 0)
                Sides.Remove(side);

            if (Sides.Count == 1)
            {
                Sides.Clear();
                foreach (Nation n in Nations.Values)
                {
                    n.WarSide = null;
                }
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
