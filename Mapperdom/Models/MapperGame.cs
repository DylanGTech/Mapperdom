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


        public MapperGame(WriteableBitmap map, int? seed = null)
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

            for (int y = 0; y < baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < baseImage.PixelWidth; x++)
                {
                     Pixels[x, y].IsGained = false;
                    //Ocean
                    if (imageArray[4 * (y * baseImage.PixelHeight + x)] == 0 //Blue
                        && imageArray[4 * (y * baseImage.PixelHeight + x) + 1] == 0 //Green
                        && imageArray[4 * (y * baseImage.PixelHeight + x) + 2] == 0) //Red
                    {
                        Pixels[x, y].IsOcean = true;
                    }
                    //Land
                    else if (imageArray[4 * (y * baseImage.PixelHeight + x)] == 255 //Blue
                             && imageArray[4 * (y * baseImage.PixelHeight + x) + 1] == 255 //Green
                             && imageArray[4 * (y * baseImage.PixelHeight + x) + 2] == 255) //Red
                    {
                        Pixels[x, y].IsOcean = false;
                        if(y < baseImage.PixelHeight / 2)
                        {
                            Pixels[x, y].OwnerId = 0;
                            Pixels[x, y].OccupierId = 0;

                        }
                        else
                        {
                            Pixels[x, y].OwnerId = 1;
                            Pixels[x, y].OccupierId = 1;
                        }
                    }
                    //Invalid
                    else
                        throw new Exception(
                            $"Image composition contains invalid coloring: R:{imageArray[4 * (y * baseImage.PixelHeight + x) + 2]},G:{imageArray[4 * (y * baseImage.PixelHeight + x) + 1]},B:{imageArray[4 * (y * baseImage.PixelHeight + x)]}");
                }
            }

            Nations = new Dictionary<byte, Nation>();

            //Default nations
            Nations.Add(0, new Nation("Rogopia", System.Drawing.Color.FromArgb(0x0000B33C)));
            Nations.Add(1, new Nation("Mechadia", System.Drawing.Color.FromArgb(0x0000B33C)));
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
                    if (Pixels[x, y].IsOcean)
                    {
                        imageArray[4 * (y * baseImage.PixelHeight + x)] = 126; // Blue
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = 56; // Green
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = 39; // Red
                        continue;
                    }
                    Nation officialNation = Nations[Pixels[x, y].OwnerId];
                    Nation occupierNation = Nations[Pixels[x, y].OccupierId];


                    if(IsTreatyMode && officialNation.IsSelected)
                    {
                        imageArray[4 * (y * baseImage.PixelHeight + x)] = 255; // Blue
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = 255; // Green
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = 255; // Red

                        continue;
                    }

                    if (!officialNation.WarSide.HasValue || !occupierNation.WarSide.HasValue)
                    {
                        if(officialNation.IsSelected)
                        {
                            imageArray[4 * (y * baseImage.PixelHeight + x)] = 255; // Blue
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = 255; // Green
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = 255; // Red
                        }
                        else
                        {
                            imageArray[4 * (y * baseImage.PixelHeight + x)] = officialNation.MainColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = officialNation.MainColor.G; // Green
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = officialNation.MainColor.R; // Red
                        }
                        continue;
                    }
                    WarSide officialSide = Sides[officialNation.WarSide.Value];
                    WarSide occupierSide = Sides[occupierNation.WarSide.Value];

                    if (Pixels[x, y].IsGained) //Was this the most recent capture?
                    {
                        imageArray[4 * (y * baseImage.PixelHeight + x)] = occupierSide.GainColor.B; // Blue
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = occupierSide.GainColor.G; // Green
                        imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = occupierSide.GainColor.R; // Red
                        //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                    }
                    else if (officialNation.Master == null && officialNation == occupierNation
                    ) //Nation controls its own territory and is not puppetted
                    {
                        if (officialNation.IsSelected)
                        {
                            imageArray[4 * (y * baseImage.PixelHeight + x)] = 255; // Blue
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = 255; // Green
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = 255; // Red
                        }
                        else
                        {
                            imageArray[4 * (y * baseImage.PixelHeight + x)] = officialSide.MainColor.B; // Blue
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 1] = officialSide.MainColor.G; // Green
                            imageArray[4 * (y * baseImage.PixelHeight + x) + 2] = officialSide.MainColor.R; // Red
                            //imageArray[4 * (y * baseImage.PixelHeight + x) + 3] = 0; // Opacity
                        }
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

        public void Advance()
        {
            ClearGains(); //Clears the resources from the last turn

            if(IsTreatyMode)
            {

            }
            else
            {
                foreach (KeyValuePair<byte, Nation> n in Nations) //Key is Nation ID, Value is the Nation object
                {
                    //Nation.plan contains all the info passes to the "Expand" function when the turn is taken
                    Expand(n.Key, n.Value.plan);
                }
            }

            CheckForCollapse(); //Did a nation collapse?
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

        public void Expand(byte ownerId, AttackInitiative plan)
        {
            //Nation is not at war. Cannot expand this way
            if (!Nations[ownerId].WarSide.HasValue)
                return;


            Queue<Point> bounds = new Queue<Point>();
            Queue<Point> nextBounds = new Queue<Point>();

            if (plan.xFocus > 3) plan.xFocus = 3;
            if (plan.xFocus < -3) plan.xFocus = -3;

            if (plan.yFocus > 3) plan.yFocus = 3;
            if (plan.yFocus < -3) plan.yFocus = -3;

            WarSide ownerSide = Sides[Nations[ownerId].WarSide.Value];

            GetBoundaryPixels(ownerId, ref bounds);
            while (plan.range > 0)
            {
                while (bounds.Count > 0)
                {
                    Point p = bounds.Dequeue();

                    if (Pixels[p.X, p.Y].IsOcean) continue;
                    byte friendlyNumber = 0; //The number of friendly Pixels surrounding it (sea, nation, or allied)

                    if (p.X > 0)
                    {
                        if (Pixels[p.X - 1, p.Y].OccupierId != ownerId && !Pixels[p.X - 1, p.Y].IsOcean &&
                            Nations[Pixels[p.X - 1, p.Y].OccupierId].WarSide.HasValue &&
                            Nations[Pixels[p.X - 1, p.Y].OccupierId].WarSide != Nations[ownerId].WarSide &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + plan.xFocus, 2) == 0)
                        {
                            Pixels[p.X - 1, p.Y].OccupierId = ownerId;
                            nextBounds.Enqueue(new Point(p.X - 1, p.Y));
                            Pixels[p.X - 1, p.Y].IsGained = true;
                            friendlyNumber++;
                        }
                        else if (Pixels[p.X - 1, p.Y].IsOcean || Pixels[p.X - 1, p.Y].OccupierId == ownerId)
                            friendlyNumber++;

                        if (plan.navalActivity && Pixels[p.X - 1, p.Y].IsOcean &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + plan.xFocus, 2) == 0)
                        {
                            if (rng.Next(0, 4) == 0)
                                AttemptNavalLanding(ownerId, new Point(p.X, p.Y), -1, 0);
                        }
                    }
                    else friendlyNumber++;

                    if (p.X < Pixels.GetLength(0) - 1)
                    {
                        if (Pixels[p.X + 1, p.Y].OccupierId != ownerId && !Pixels[p.X + 1, p.Y].IsOcean &&
                            Nations[Pixels[p.X + 1, p.Y].OccupierId].WarSide.HasValue &&
                            Nations[Pixels[p.X + 1, p.Y].OccupierId].WarSide != Nations[ownerId].WarSide &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 - plan.xFocus, 2) == 0)
                        {
                            Pixels[p.X + 1, p.Y].OccupierId = ownerId;
                            nextBounds.Enqueue(new Point(p.X + 1, p.Y));
                            Pixels[p.X + 1, p.Y].IsGained = true;
                            friendlyNumber++;
                        }
                        else if (Pixels[p.X + 1, p.Y].IsOcean || Pixels[p.X + 1, p.Y].OccupierId == ownerId)
                            friendlyNumber++;

                        if (plan.navalActivity && Pixels[p.X + 1, p.Y].IsOcean &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + plan.xFocus, 2) == 0)
                        {
                            if (rng.Next(0, 4) == 0)
                                AttemptNavalLanding(ownerId, new Point(p.X, p.Y), 1, 0);
                        }
                    }
                    else friendlyNumber++;

                    if (p.Y > 0)
                    {
                        if (Pixels[p.X, p.Y - 1].OccupierId != ownerId && !Pixels[p.X, p.Y - 1].IsOcean &&
                            Nations[Pixels[p.X, p.Y - 1].OccupierId].WarSide.HasValue &&
                            Nations[Pixels[p.X, p.Y - 1].OccupierId].WarSide != Nations[ownerId].WarSide &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + plan.yFocus, 2) == 0)
                        {
                            Pixels[p.X, p.Y - 1].OccupierId = ownerId;
                            nextBounds.Enqueue(new Point(p.X, p.Y - 1));
                            Pixels[p.X, p.Y - 1].IsGained = true;
                            friendlyNumber++;
                        }
                        else if (Pixels[p.X, p.Y - 1].IsOcean || Pixels[p.X, p.Y - 1].OccupierId == ownerId)
                            friendlyNumber++;

                        if (plan.navalActivity && Pixels[p.X, p.Y - 1].IsOcean &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + plan.yFocus, 2) == 0)
                        {
                            if (rng.Next(0, 4) == 0)
                                AttemptNavalLanding(ownerId, new Point(p.X, p.Y), 0, -1);
                        }
                    }
                    else friendlyNumber++;

                    if (p.Y < Pixels.GetLength(1) - 1)
                    {
                        if (Pixels[p.X, p.Y + 1].OccupierId != ownerId && !Pixels[p.X, p.Y + 1].IsOcean &&
                            Nations[Pixels[p.X, p.Y + 1].OccupierId].WarSide.HasValue &&
                            Nations[Pixels[p.X, p.Y + 1].OccupierId].WarSide != Nations[ownerId].WarSide &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 - plan.yFocus, 2) == 0)
                        {
                            Pixels[p.X, p.Y + 1].OccupierId = ownerId;
                            nextBounds.Enqueue(new Point(p.X, p.Y + 1));
                            Pixels[p.X, p.Y + 1].IsGained = true;
                            friendlyNumber++;
                        }
                        else if (Pixels[p.X, p.Y + 1].IsOcean || Pixels[p.X, p.Y + 1].OccupierId == ownerId)
                            friendlyNumber++;

                        if (plan.navalActivity && Pixels[p.X, p.Y + 1].IsOcean &&
                            (byte) rng.Next(0, 255) % Math.Pow(4 + plan.yFocus, 2) == 0)
                        {
                            if (rng.Next(0, 4) == 0)
                                AttemptNavalLanding(ownerId, new Point(p.X, p.Y), 0, 1);
                        }
                    }
                    else friendlyNumber++;

                    //If not all Pixels are friendly, save this pixel for the next expansion
                    if (friendlyNumber < 4) nextBounds.Enqueue(p);
                }

                plan.range--;
                bounds = nextBounds;
                nextBounds = new Queue<Point>();
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


            PreviousStates.AddLast(new MapState((PixelData[,])Pixels.Clone(), clonedNations, clonedSides));

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
            PreviousStatesPosition++;
        }

        public void DeclareWar(byte nation1Id, byte nation2Id, WarSide nation1Side, WarSide nation2Side)
        {
            byte newWarSideId = 0;

            if(!Nations[nation1Id].WarSide.HasValue)
            {
                if(!Sides.ContainsValue(nation1Side))
                {
                    while (Sides.ContainsKey(newWarSideId)) newWarSideId++;

                    Sides.Add(newWarSideId, nation1Side);
                }

                Nations[nation1Id].WarSide = newWarSideId;
            }
            if (!Nations[nation2Id].WarSide.HasValue)
            {
                if (!Sides.ContainsValue(nation2Side))
                {
                    while (Sides.ContainsKey(newWarSideId)) newWarSideId++;

                    Sides.Add(newWarSideId, nation2Side);
                }

                Nations[nation2Id].WarSide = newWarSideId;
            }
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
            if (!Sides.ContainsValue(rebelSide))
            {
                while (Sides.ContainsKey(newWarSideId)) newWarSideId++;

                Nations.Add(newNationId, rebels);
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
                } while (!Pixels[x1, y1].IsOcean && Pixels[x1, y1].OwnerId == nationID);

                Pixels[x1, y1].OwnerId = newNationId;
            }
        }
    }
}
