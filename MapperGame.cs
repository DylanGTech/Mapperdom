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

namespace Mapperdom
{
    public class MapperGame
    {
        private readonly WriteableBitmap baseImage;

        //Null represents an unclaimable pixel (ocean)
        private byte?[,] ownershipData; //Determines which nation officially owns a pixel (including puppet states)
        private byte?[,] occupationData; //Determines which nation occupies a pixel (excluding puppet states)
        private bool[,] newCapturesData;

        private byte?[,] ownershipDataBackup;
        private byte?[,] occupationDataBackup;
        private bool[,] newCapturesDataBackup;

        public Dictionary<byte, Nation> Nations;
        private Dictionary<byte, Nation> nationsBackup;

        public Dictionary<byte, WarSide> Sides;
        private Dictionary<byte, WarSide> sidesBackup;

        private readonly Random rng;

        public MapperGame(WriteableBitmap map, int? seed = null)
        {
            rng = seed != null ? new Random(seed.Value) : new Random();

            baseImage = map;

            byte[] imageArray = new byte[baseImage.PixelHeight * baseImage.PixelWidth * 4];

            using (Stream stream = baseImage.PixelBuffer.AsStream())
            {
                stream.Read(imageArray, 0, imageArray.Length);
            }

            ownershipData = new byte?[baseImage.PixelWidth, baseImage.PixelHeight];
            occupationData = new byte?[baseImage.PixelWidth, baseImage.PixelHeight];
            newCapturesData = new bool[baseImage.PixelWidth, baseImage.PixelHeight];

            ownershipDataBackup = new byte?[baseImage.PixelWidth, baseImage.PixelHeight];
            occupationDataBackup = new byte?[baseImage.PixelWidth, baseImage.PixelHeight];
            newCapturesDataBackup = new bool[baseImage.PixelWidth, baseImage.PixelHeight];

            Sides = new Dictionary<byte, WarSide>();
            sidesBackup = new Dictionary<byte, WarSide>();

            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {
                    newCapturesData[x, y] = false;
                    //Ocean
                    if (imageArray[4 * (y * baseImage.PixelHeight + x)] == 0x80 //Blue
                        && imageArray[4 * (y * baseImage.PixelHeight + x) + 1] == 0x55 //Green
                        && imageArray[4 * (y * baseImage.PixelHeight + x) + 2] == 0x00) //Red
                    {
                        ownershipData[x, y] = null;
                        occupationData[x, y] = null;
                    }
                    //Land
                    else if (imageArray[4 * (y * baseImage.PixelHeight + x)] == 0x00 //Blue
                             && imageArray[4 * (y * baseImage.PixelHeight + x) + 1] == 0xB3 //Green
                             && imageArray[4 * (y * baseImage.PixelHeight + x) + 2] == 0x3C) //Red
                    {
                        ownershipData[x, y] = 0;
                        occupationData[x, y] = 0;
                    }
                    //Invalid
                    else
                        throw new Exception(
                            $"Image composition contains invalid coloring: R:{imageArray[4 * (y * baseImage.PixelHeight + x) + 2]},G:{imageArray[4 * (y * baseImage.PixelHeight + x) + 1]},B:{imageArray[4 * (y * baseImage.PixelHeight + x)]}");
                }
            }

            Nations = new Dictionary<byte, Nation>();
            nationsBackup = new Dictionary<byte, Nation>();

            //Default nation
            Nations.Add(0, new Nation("Rogopia"));
            //nations.Add(1, new Nation("Mechadia"));

            //sides.Add(0, new WarSide("Northerners", Nation.ColorFromHSL(0f, 0.6f, 0.5f)));
            //sides.Add(1, new WarSide("Southerners", Nation.ColorFromHSL(120f, 0.6f, 0.5f)));
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
                    if (!ownershipData[x, y].HasValue) continue;
                    Nation officialNation = Nations[ownershipData[x, y].Value];
                    Nation occupierNation = Nations[occupationData[x, y].Value];


                    if (!officialNation.WarSide.HasValue || !occupierNation.WarSide.HasValue) continue;
                    WarSide officialSide = Sides[officialNation.WarSide.Value];
                    WarSide occupierSide = Sides[occupierNation.WarSide.Value];

                    if (newCapturesData[x, y]) //Was this the most recent capture?
                    {
                        imageArray[4 * (y * baseImage.PixelHeight + x)] = occupierSide.GainColor.B; // Blue
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = occupierSide.GainColor.G; // Green
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = occupierSide.GainColor.R; // Red
                        //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                    }
                    else if (officialNation.Master == null && officialNation == occupierNation
                    ) //Nation controls its own territory and is not puppetted
                    {
                        imageArray[4 * (y * baseImage.PixelHeight + x)] = officialSide.MainColor.B; // Blue
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = officialSide.MainColor.G; // Green
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = officialSide.MainColor.R; // Red
                        //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                    }
                    else if (officialNation.Master == occupierNation || officialNation == occupierNation
                    ) //Nation controlls a puppet's territory
                    {
                        imageArray[4 * (y * baseImage.PixelHeight + x)] = occupierSide.PuppetColor.B; // Blue
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 1] =
                            occupierSide.PuppetColor.G; // Green
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = occupierSide.PuppetColor.R; // Red
                        //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                    }
                    else //Nation is occupied by another nation
                    {
                        imageArray[4 * (y * baseImage.PixelHeight + x)] = occupierSide.OccupiedColor.B; // Blue
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 1] =
                            occupierSide.OccupiedColor.G; // Green
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 2] =
                            occupierSide.OccupiedColor.R; // Red
                        //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                    }
                }
            }

            GetBorders(ref borders);
            while (borders.Count > 0)
            {
                Point p = borders.Dequeue();

                imageArray[4 * (p.Y * baseImage.PixelHeight + p.X)] = 0; // Blue
                imageArray[4 * (p.Y * baseImage.PixelHeight + p.X) + 1] = 0; // Green
                imageArray[4 * (p.Y * baseImage.PixelHeight + p.X) + 2] = 0; // Red
                //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
            }
        }

        public void Advance(ushort amount, byte nationId, sbyte xFocus = 0, sbyte yFocus = 0)
        {
            ClearGains();
            Expand(amount, nationId, xFocus, yFocus);
            CheckForCollapse();
        }

        public void AnnexTerritory(byte nationId)
        {
            AnnexOccupation(nationId);
        }

        public void Surrender(byte nationId)
        {
            ClearGains();
            //TODO: Make nations surrender to those they are warring with, not the first avialable one in the dictionary
            foreach (byte n in Nations.Keys)
            {
                if (n == nationId) continue;
                Surrender(nationId, n);
                break;
            }
        }

        //Get pixels with locations that border other nations (not including the sea)
        public void GetBorders(ref Queue<Point> borders)
        {
            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {
                    if (!ownershipData[x, y].HasValue) continue;
                    if (x > 0 && ownershipData[x - 1, y].HasValue && ownershipData[x - 1, y] != ownershipData[x, y])
                    {
                        borders.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (x < ownershipData.GetLength(0) - 1 && ownershipData[x + 1, y].HasValue &&
                        ownershipData[x + 1, y] != ownershipData[x, y])
                    {
                        borders.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y > 0 && ownershipData[x, y - 1].HasValue && ownershipData[x, y - 1] != ownershipData[x, y])
                    {
                        borders.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y >= ownershipData.GetLength(1) - 1 || !ownershipData[x, y + 1].HasValue ||
                        ownershipData[x, y + 1] == ownershipData[x, y]) continue;
                    borders.Enqueue(new Point(x, y));
                    continue;
                }
            }
        }

        public void Expand(ushort range, byte ownerId, sbyte xFocus = 0, sbyte yFocus = 0, bool navalActivity = true)
        {
            //Nation is not at war. Cannot expand this way
            if (!Nations[ownerId].WarSide.HasValue)
                return;

            Queue<Point> bounds = new Queue<Point>();
            Queue<Point> nextBounds = new Queue<Point>();

            if (xFocus > 3) xFocus = 3;
            if (xFocus < -3) xFocus = -3;

            if (yFocus > 3) yFocus = 3;
            if (yFocus < -3) yFocus = -3;

            WarSide ownerSide = Sides[Nations[ownerId].WarSide.Value];

            GetBoundaryPixels(ownerId, ref bounds);
            while (range > 0)
            {
                while (bounds.Count > 0)
                {
                    Point p = bounds.Dequeue();

                    if (!occupationData[p.X, p.Y].HasValue) continue;
                    byte friendlyNumber = 0; //The number of friendly pixels surrounding it (sea, nation, or allied)

                    if (p.X > 0)
                    {
                        if (occupationData[p.X - 1, p.Y] != ownerId && occupationData[p.X - 1, p.Y].HasValue &&
                            Nations[occupationData[p.X - 1, p.Y].Value].WarSide.HasValue &&
                            Nations[occupationData[p.X - 1, p.Y].Value].WarSide != Nations[ownerId].WarSide &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + xFocus, 2) == 0)
                        {
                            occupationData[p.X - 1, p.Y] = ownerId;
                            nextBounds.Enqueue(new Point(p.X - 1, p.Y));
                            newCapturesData[p.X - 1, p.Y] = true;
                            friendlyNumber++;
                        }
                        else if (!occupationData[p.X - 1, p.Y].HasValue || occupationData[p.X - 1, p.Y] == ownerId)
                            friendlyNumber++;

                        if (navalActivity && !occupationData[p.X - 1, p.Y].HasValue &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + xFocus, 2) == 0)
                        {
                            if (rng.Next(0, 4) == 0)
                                AttemptNavalLanding(ownerId, new Point(p.X, p.Y), -1, 0);
                        }
                    }
                    else friendlyNumber++;

                    if (p.X < occupationData.GetLength(0) - 1)
                    {
                        if (occupationData[p.X + 1, p.Y] != ownerId && occupationData[p.X + 1, p.Y].HasValue &&
                            Nations[occupationData[p.X + 1, p.Y].Value].WarSide.HasValue &&
                            Nations[occupationData[p.X + 1, p.Y].Value].WarSide != Nations[ownerId].WarSide &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 - xFocus, 2) == 0)
                        {
                            occupationData[p.X + 1, p.Y] = ownerId;
                            nextBounds.Enqueue(new Point(p.X + 1, p.Y));
                            newCapturesData[p.X + 1, p.Y] = true;
                            friendlyNumber++;
                        }
                        else if (!occupationData[p.X + 1, p.Y].HasValue || occupationData[p.X + 1, p.Y] == ownerId)
                            friendlyNumber++;

                        if (navalActivity && !occupationData[p.X + 1, p.Y].HasValue &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + xFocus, 2) == 0)
                        {
                            if (rng.Next(0, 4) == 0)
                                AttemptNavalLanding(ownerId, new Point(p.X, p.Y), 1, 0);
                        }
                    }
                    else friendlyNumber++;

                    if (p.Y > 0)
                    {
                        if (occupationData[p.X, p.Y - 1] != ownerId && occupationData[p.X, p.Y - 1].HasValue &&
                            Nations[occupationData[p.X, p.Y - 1].Value].WarSide.HasValue &&
                            Nations[occupationData[p.X, p.Y - 1].Value].WarSide != Nations[ownerId].WarSide &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + yFocus, 2) == 0)
                        {
                            occupationData[p.X, p.Y - 1] = ownerId;
                            nextBounds.Enqueue(new Point(p.X, p.Y - 1));
                            newCapturesData[p.X, p.Y - 1] = true;
                            friendlyNumber++;
                        }
                        else if (!occupationData[p.X, p.Y - 1].HasValue || occupationData[p.X, p.Y - 1] == ownerId)
                            friendlyNumber++;

                        if (navalActivity && !occupationData[p.X, p.Y - 1].HasValue &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + yFocus, 2) == 0)
                        {
                            if (rng.Next(0, 4) == 0)
                                AttemptNavalLanding(ownerId, new Point(p.X, p.Y), 0, -1);
                        }
                    }
                    else friendlyNumber++;

                    if (p.Y < occupationData.GetLength(1) - 1)
                    {
                        if (occupationData[p.X, p.Y + 1] != ownerId && occupationData[p.X, p.Y + 1].HasValue &&
                            Nations[occupationData[p.X, p.Y + 1].Value].WarSide.HasValue &&
                            Nations[occupationData[p.X, p.Y + 1].Value].WarSide != Nations[ownerId].WarSide &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 - yFocus, 2) == 0)
                        {
                            occupationData[p.X, p.Y + 1] = ownerId;
                            nextBounds.Enqueue(new Point(p.X, p.Y + 1));
                            newCapturesData[p.X, p.Y + 1] = true;
                            friendlyNumber++;
                        }
                        else if (!occupationData[p.X, p.Y + 1].HasValue || occupationData[p.X, p.Y + 1] == ownerId)
                            friendlyNumber++;

                        if (navalActivity && !occupationData[p.X, p.Y + 1].HasValue &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + yFocus, 2) == 0)
                        {
                            if (rng.Next(0, 4) == 0)
                                AttemptNavalLanding(ownerId, new Point(p.X, p.Y), 0, 1);
                        }
                    }
                    else friendlyNumber++;

                    //If not all pixels are friendly, save this pixel for the next expansion
                    if (friendlyNumber < 4) nextBounds.Enqueue(p);
                }

                range--;
                bounds = nextBounds;
                nextBounds = new Queue<Point>();
            }
        }

        public void Surrender(byte destroyedNationId, byte destroyerNationId)
        {
            for (int y = 0; y < occupationData.GetLength(1); y++)
            {
                for (int x = 0; x < occupationData.GetLength(0); x++)
                {
                    if (occupationData[x, y] != destroyedNationId) continue;
                    occupationData[x, y] = destroyerNationId;
                    newCapturesData[x, y] = true;
                }
            }
        }

        public void CheckForCollapse()
        {
            List<byte> nationsToKeep = new List<byte>();
            List<byte> nationsToRemove = new List<byte>();

            for (int y = 0; y < ownershipData.GetLength(1); y++)
            {
                for (int x = 0; x < ownershipData.GetLength(0); x++)
                {
                    if (ownershipData[x, y].HasValue && !nationsToKeep.Contains(ownershipData[x, y].Value))
                        nationsToKeep.Add(ownershipData[x, y].Value);
                    if (occupationData[x, y].HasValue && !nationsToKeep.Contains(occupationData[x, y].Value))
                        nationsToKeep.Add(occupationData[x, y].Value);
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
        }

        public void Collapse(byte destroyedNationId, byte destroyerNationId)
        {
            for (int y = 0; y < occupationData.GetLength(1); y++)
            {
                for (int x = 0; x < occupationData.GetLength(0); x++)
                {
                    if (occupationData[x, y] == destroyedNationId)
                    {
                        occupationData[x, y] = destroyedNationId;
                        newCapturesData[x, y] = true;
                    }

                    if (ownershipData[x, y] == destroyedNationId)
                        ownershipData[x, y] = destroyerNationId;
                }
            }
        }

        private void ClearGains()
        {
            for (int y = 0; y < newCapturesData.GetLength(1); y++)
            {
                for (int x = 0; x < newCapturesData.GetLength(0); x++)
                {
                    newCapturesData[x, y] = false;
                }
            }
        }

        private void GetBoundaryPixels(byte owner, ref Queue<Point> bounds)
        {
            for (int y = 0; y < occupationData.GetLength(1); y++)
            {
                for (int x = 0; x < occupationData.GetLength(0); x++)
                {
                    if (occupationData[x, y] == null || occupationData[x, y].Value != owner) continue;
                    if (x > 0 && occupationData[x - 1, y] != owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (x < occupationData.GetLength(0) - 1 && occupationData[x + 1, y] != owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y > 0 && occupationData[x, y - 1] != owner)
                    {
                        bounds.Enqueue(new Point(x, y));
                        continue;
                    }

                    if (y >= occupationData.GetLength(1) - 1 || occupationData[x, y + 1] == owner) continue;
                    bounds.Enqueue(new Point(x, y));
                }
            }
        }

        private void AttemptNavalLanding(byte conqueror, Point p, sbyte xDir, sbyte yDir)
        {
            if (xDir > 1) xDir = 1;
            else if (xDir < -1) xDir = -1;
            if (yDir > 1) yDir = 1;
            else if (yDir < -1) yDir = -1;

            while (p.X > 0 && p.Y > 0 && p.X < occupationData.GetLength(0) - 1 && p.Y < occupationData.GetLength(1) - 1)
            {
                p.X += xDir;
                p.Y += yDir;

                if (!occupationData[p.X, p.Y].HasValue) continue;
                if (Nations[occupationData[p.X, p.Y].Value].WarSide.HasValue &&
                    Nations[occupationData[p.X, p.Y].Value].WarSide != Nations[conqueror].WarSide)
                    occupationData[p.X, p.Y] = conqueror;
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


            for (int y = 0; y < occupationData.GetLength(1); y++)
            {
                for (int x = 0; x < occupationData.GetLength(0); x++)
                {
                    //if (bounds[x, y])
                    if (occupationData[x, y] == ownerId)
                        ownershipData[x, y] = ownerId;
                }
            }
        }

        //Recursively fill in surrounding pixels with data until it hits a border of some kind
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
            for (int y = 0; y < occupationData.GetLength(1); y++)
            {
                for (int x = 0; x < occupationData.GetLength(0); x++)
                {
                    newCapturesDataBackup[x, y] = newCapturesData[x, y];
                    occupationDataBackup[x, y] = occupationData[x, y];
                    ownershipDataBackup[x, y] = ownershipData[x, y];
                }
            }

            nationsBackup.Clear();
            foreach (var (key, value) in Nations)
            {
                nationsBackup.Add(key, value);
            }

            sidesBackup.Clear();
            foreach (var (key, value) in Sides)
            {
                sidesBackup.Add(key, value);
            }
        }

        public void Restore()
        {
            for (int y = 0; y < occupationData.GetLength(1); y++)
            {
                for (int x = 0; x < occupationData.GetLength(0); x++)
                {
                    newCapturesData[x, y] = newCapturesDataBackup[x, y];
                    occupationData[x, y] = occupationDataBackup[x, y];
                    ownershipData[x, y] = ownershipDataBackup[x, y];
                }
            }

            Nations.Clear();
            foreach (var (key, value) in nationsBackup)
            {
                Nations.Add(key, value);
            }

            Sides.Clear();
            foreach (var (key, value) in sidesBackup)
            {
                Sides.Add(key, value);
            }
        }

        public void SwapBanks()
        {
            var swapper1 = occupationDataBackup;
            occupationDataBackup = occupationData;
            occupationData = swapper1;

            swapper1 = ownershipDataBackup;
            ownershipDataBackup = ownershipData;
            ownershipData = swapper1;

            var swapper2 = newCapturesDataBackup;
            newCapturesDataBackup = newCapturesData;
            newCapturesData = swapper2;

            var swapper3 = nationsBackup;
            nationsBackup = Nations;
            Nations = swapper3;

            var swapper4 = sidesBackup;
            sidesBackup = Sides;
            Sides = swapper4;
        }

        public void StartUprising(byte nationID)
        {
            uint numpixels = 0;
            for (int y = 0; y < ownershipData.GetLength(1); y++)
            {
                for (int x = 0; x < ownershipData.GetLength(0); x++)
                {
                    if (ownershipData[x, y] == nationID) numpixels++;
                }
            }

            if (numpixels < 128) //Not a large enough nation for a rebellion to start
                return;

            byte newNationId = 0;
            byte newWarSideId = 0;
            while (Nations.ContainsKey(newNationId)) newNationId++;
            while (Sides.ContainsKey(newWarSideId)) newWarSideId++;

            Nations.Add(newNationId, new Nation("Rebels"));
            float f = rng.Next(0, 360);
            Sides.Add(newWarSideId, new WarSide("Partisan Forces", Nation.ColorFromHsl(f, 0.6f, 0.5f)));
            Nations[newNationId].WarSide = newWarSideId;

            if (!Nations[nationID].WarSide.HasValue)
            {
                newWarSideId = 0;
                while (Sides.ContainsKey(newWarSideId)) newWarSideId++;
                f = rng.Next(0, 360);
                Sides.Add(newWarSideId, new WarSide("Establishment Forces", Nation.ColorFromHsl(f, 0.6f, 0.5f)));
                Nations[nationID].WarSide = newWarSideId;
            }

            for (byte b = 0; b < 31; b++)
            {
                int x1;
                int y1;
                do
                {
                    x1 = rng.Next(0, occupationData.GetLength(0) - 1);
                    y1 = rng.Next(0, occupationData.GetLength(1) - 1);
                } while (!occupationData[x1, y1].HasValue && ownershipData[x1, y1] == nationID);

                occupationData[x1, y1] = newNationId;
            }
        }
    }
}
