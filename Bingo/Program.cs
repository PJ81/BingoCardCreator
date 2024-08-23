using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Collections.Generic;

namespace Bingo {
    class Program {

        static List<Card> cards = new List<Card>();

        public class Card {

            public int[,] card;
            public string hash;

            public Card() {
                card = new int[5, 5];
                hash = "";
            }

            public int[] getD(int i) {
                if(i == 0) {
                    return  new int[5] { card[0, 0], card[1, 1], 0, card[3, 3], card[4, 4] };
                } else {
                    return new int[5] { card[4, 0], card[3, 1], 0, card[1, 3], card[0, 4] };
                }
            }

            public int[] getL(int i) {
                return new int[5] { card[0, i], card[1, i], 0, card[3, i], card[4, i] };
            }

            public int[] getC(int i) {
                return new int[5] { card[i, 0], card[i, 1], 0, card[i, 3], card[i, 4] };
            }
        }

        class BingoCard {

            private Random rand;

            public BingoCard() {
                rand = new Random();
            }

            private byte[] getHash(string str) {
                using (HashAlgorithm algorithm = HMACSHA1.Create())
                    return algorithm.ComputeHash(Encoding.UTF8.GetBytes(str));
            }

            private void calcHash(Card card) {
                string str = "";

                for(int j = 0; j < 5; j++) {
                    for (int i = 0; i < 5; i++) {
                        str += card.card[j, i].ToString();
                    }
                }

                card.hash = BitConverter.ToString(getHash(str)).Replace("-", String.Empty);
            }

            public Card createCard() {

                int min = 1;
                Card card = new Card();

                for(int c = 0; c < 5; c++) {
                    int max = min + 15;

                    int[] chosen = new int[5];
                    int rn;

                    for(int n = 0; n < 5; n++) {
                        
                        while(true) {
                            rn = rand.Next(min, max);
                            if (!chosen.Contains(rn)) break;
                        }

                        chosen[n] = rn;
                        card.card[c, n] = rn;
                    }

                    min += 15;
                }

                calcHash(card);

                return card;
            }

        }

        private static void createPDF(List<Card> cards) {

            PdfDocument document = new PdfDocument();
            document.Info.Title = "BingoCards";
            string bingo = "BINGO";

            XUnitPt REC_SIZE = 26, MARGIN = 2, BIG_RECT_MARGIN = 4, TOT_WID = REC_SIZE * 5 + 4 * MARGIN;

            XFont font = new XFont("verdana", REC_SIZE / 2);
            XFont fontN = new XFont("verdana", REC_SIZE * .9, XFontStyleEx.Bold);
            XFont fontS = new XFont("verdana", REC_SIZE * .3);

            XBrush[] brs = {
                 new XSolidBrush(XColor.FromArgb(255, 15, 159, 255)),
                 new XSolidBrush(XColor.FromArgb(255, 25, 214, 182)),
                 new XSolidBrush(XColor.FromArgb(255, 153, 255, 50)),
                 new XSolidBrush(XColor.FromArgb(255, 254, 131, 54)),
                 new XSolidBrush(XColor.FromArgb(255, 229, 54, 0)),
                 new XSolidBrush(XColor.FromArgb(255, 34, 64, 80))
            };

            int pageCount = (int)Math.Ceiling((double)(cards.Count / 12.0));

            for (int page = 0; page < pageCount; page++) {

                document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(document.Pages[page]);

                XUnitPt startY = 70;

                for (int rowsOnPage = 0; rowsOnPage < 4; rowsOnPage++) {

                    XUnitPt startX = 50;
                    int cardsRow = rowsOnPage * 3 + 12 * page;

                    for (int o = cardsRow; o < cardsRow + 3; o++) {

                        if (o >= cards.Count) break;

                        // output BINGO
                        for (int r = 0; r < 5; r++) {
                            XUnitPt x = startX + REC_SIZE * r + r * MARGIN, y = startY - REC_SIZE - 2;
                            XRect rect = new XRect(x, y, REC_SIZE, REC_SIZE);
                            gfx.DrawRoundedRectangle(brs[r], rect, new XSize(2, 2));
                            gfx.DrawRoundedRectangle(XPens.Black, rect, new XSize(2, 2));
                            gfx.DrawString(bingo[r].ToString(), fontN, brs[5], rect, XStringFormats.Center);
                        }

                        // output big rectangle around card
                        XRect bigRect = new XRect(startX - BIG_RECT_MARGIN, startY - BIG_RECT_MARGIN - REC_SIZE - 2, TOT_WID + 2 * BIG_RECT_MARGIN, TOT_WID + REC_SIZE + MARGIN + 2 * BIG_RECT_MARGIN);
                        gfx.DrawRoundedRectangle(XPens.Black, bigRect, new XSize(2, 2));

                        // output card
                        Card card = cards[o];

                        for (int c = 0; c < 5; c++) {
                            for (int r = 0; r < 5; r++) {

                                XUnitPt x = startX + REC_SIZE * c + c * MARGIN, y = startY + REC_SIZE * r + r * MARGIN;
                                XRect rect = new XRect(x, y, REC_SIZE, REC_SIZE);

                                gfx.DrawRoundedRectangle(XPens.Black, rect, new XSize(2, 2));

                                if(c == 2 && r == 2) {
                                    gfx.DrawString("FREE", fontS, XBrushes.Black, rect, XStringFormats.Center);

                                } else {
                                    gfx.DrawString(card.card[c, r].ToString(), font, XBrushes.Black, rect, XStringFormats.Center);

                                }

                            }

                        }
                        startX += TOT_WID + 40;

                    }
                    startY += TOT_WID + 60;

                }
            }
            
            document.Save("bingoCards.pdf");

        }

        static private bool isUnique(Card card) {
            Card fi = cards.Find(crd => crd.hash.Equals(card.hash));
            if (fi != null) return false;

            int[] d0 = card.getD(0), d1 = card.getD(1), l0 = card.getL(0), l1 = card.getL(1), l2 = card.getL(2), l3 = card.getL(3),
                l4 = card.getL(4), c0 = card.getC(0), c1 = card.getC(1), c2 = card.getC(2), c3 = card.getC(3), c4 = card.getC(4);

            foreach (Card crd in cards) {
                if (crd.getD(0).SequenceEqual(d0)) return false;
                if (crd.getD(1).SequenceEqual(d1)) return false;

                if (crd.getL(0).SequenceEqual(l0)) return false;
                if (crd.getL(1).SequenceEqual(l1)) return false;
                if (crd.getL(2).SequenceEqual(l2)) return false;
                if (crd.getL(3).SequenceEqual(l3)) return false;
                if (crd.getL(4).SequenceEqual(l4)) return false;

                if (crd.getC(0).SequenceEqual(c0)) return false;
                if (crd.getC(1).SequenceEqual(c1)) return false;
                if (crd.getC(2).SequenceEqual(c2)) return false;
                if (crd.getC(3).SequenceEqual(c3)) return false;
                if (crd.getC(4).SequenceEqual(c4)) return false;
            }

            return true;
        }

        static void Main(string[] args) {
            BingoCard bc = new BingoCard();

            const int cnt = 120;
            int err = 0;
            Card  card;

            for (int i = 0; i < cnt; i++) {
                while (true) {
                    card = bc.createCard();

                    if (isUnique(card)) { 
                        cards.Add(card);
                        break;
                    } else {
                        err++;
                    }
                }
            }

            createPDF(cards);

            if(err > 0) {
                Console.WriteLine($"Errors: {err}");
                Console.ReadKey();
            }
        }
    }
}

