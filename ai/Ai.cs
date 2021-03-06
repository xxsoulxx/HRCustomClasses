﻿using System;
using System.Collections.Generic;
using System.Text;

namespace HREngine.Bots
{

    public class Ai
    {
        List<Playfield> posmoves = new List<Playfield>();

        Hrtprozis hp = Hrtprozis.Instance;
        Handmanager hm = Handmanager.Instance;
        Helpfunctions help = Helpfunctions.Instance;

        public Action bestmove = new Action();

        private static Ai instance;

        public static Ai Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Ai();
                }
                return instance;
            }
        }

        private Ai()
        {
        }

        private bool doAllChoices(CardDB.Card card, Playfield p, Handmanager.Handcard hc)
        {
            bool havedonesomething = false;

            for (int i = 1; i < 3; i++)
            {
                CardDB.Card c = card;
                if (card.name == "sternenregen")
                {
                    if (i == 1)
                    {
                        c = CardDB.Instance.getCardDataFromID("NEW1_007b");
                    }
                    if (i == 2)
                    {
                        c = CardDB.Instance.getCardDataFromID("NEW1_007a");
                    }
                }

                if (card.name == "urtumderlehren")
                {
                    if (i == 1)
                    {
                        c = CardDB.Instance.getCardDataFromID("NEW1_008a");
                    }
                    if (i == 2)
                    {
                        c = CardDB.Instance.getCardDataFromID("NEW1_008b");
                    }
                }

                if (c.canplayCard(p))
                {
                    havedonesomething = true;




                    List<targett> trgts = c.getTargetsForCard(p);

                    if (trgts.Count == 0)
                    {
                        Playfield pf = new Playfield(p);

                        pf.playCard(card, hc.position - 1, hc.entity, -1, -1, i);
                        this.posmoves.Add(pf);
                    }
                    else
                    {
                        foreach (targett trgt in trgts)
                        {
                            Playfield pf = new Playfield(p);
                            pf.playCard(card, hc.position - 1, hc.entity, trgt.target, trgt.targetEntity, i);
                            this.posmoves.Add(pf);
                        }
                    }

                }

            }


            return havedonesomething;
        }



        private void doallmoves(bool test, BotBase botBase)
        {

            bool havedonesomething = true;
            List<Playfield> temp = new List<Playfield>();
            int deep = 0;
            while (havedonesomething)
            {
                help.logg("ailoop");
                temp.Clear();
                temp.AddRange(this.posmoves);
                havedonesomething = false;
                Playfield bestold = null;
                int bestoldval = -20000000;
                foreach (Playfield p in temp)
                {

                    if (p.complete)
                    {
                        continue;
                    }

                    //take a card and play it
                    List<string> playedcards = new List<string>();
                    foreach (Handmanager.Handcard hc in p.owncards)
                    {
                        CardDB.Card c = hc.card;
                        //help.logg("try play crd" + c.name + " " + c.getManaCost(p) + " " + c.canplayCard(p));
                        if (playedcards.Contains(c.name)) continue; // dont play the same card in one loop
                        playedcards.Add(c.name);
                        if (c.choice)
                        {
                            if (doAllChoices(c, p, hc))
                            {
                                havedonesomething = true;
                            }
                        }
                        else
                        {

                            if (c.canplayCard(p))
                            {
                                havedonesomething = true;
                                List<targett> trgts = c.getTargetsForCard(p);

                                if (trgts.Count == 0)
                                {
                                    Playfield pf = new Playfield(p);
                                    pf.playCard(c, hc.position - 1, hc.entity, -1, -1, 0);
                                    this.posmoves.Add(pf);
                                }
                                else
                                {
                                    foreach (targett trgt in trgts)
                                    {
                                        Playfield pf = new Playfield(p);
                                        pf.playCard(c, hc.position - 1, hc.entity, trgt.target, trgt.targetEntity, 0);
                                        this.posmoves.Add(pf);
                                    }

                                }


                            }
                        }
                    }

                    //attack with a minion
                    foreach (Minion m in p.ownMinions)
                    {

                        if (m.Ready && m.Angr >= 1 && !m.frozen)
                        {
                            List<targett> trgts = p.getAttackTargets();
                            havedonesomething = true;
                            foreach (targett trgt in trgts)
                            {
                                Playfield pf = new Playfield(p);
                                pf.attackWithMinion(m, trgt.target, trgt.targetEntity);
                                this.posmoves.Add(pf);
                            }

                        }

                    }

                    // attack with hero
                    if (p.ownHeroReady)
                    {
                        List<targett> trgts = p.getAttackTargets();
                        havedonesomething = true;
                        foreach (targett trgt in trgts)
                        {
                            Playfield pf = new Playfield(p);
                            pf.attackWithWeapon(trgt.target, trgt.targetEntity);
                            this.posmoves.Add(pf);
                        }
                    }

                    // use ability
                    /// TODO check if ready after manaup
                    if (p.ownAbilityReady && p.mana >= 2)
                    {
                        havedonesomething = true;
                        if (this.hp.heroname == "mage" || this.hp.heroname == "priest")
                        {

                            List<targett> trgts = p.ownHeroAblility.getTargetsForCard(p);
                            foreach (targett trgt in trgts)
                            {
                                //if (this.hp.heroname == "priest" && trgt == 200) continue;
                                havedonesomething = true;
                                Playfield pf = new Playfield(p);
                                pf.activateAbility(p.ownHeroAblility, this.hp.heroname, trgt.target, trgt.targetEntity);
                                this.posmoves.Add(pf);
                            }
                        }
                        else
                        {
                            havedonesomething = true;
                            Playfield pf = new Playfield(p);
                            pf.activateAbility(p.ownHeroAblility, this.hp.heroname, -1, -1);
                            this.posmoves.Add(pf);
                        }

                    }


                    p.complete = true;

                    //sort stupid stuff ouf

                    if (botBase.getPlayfieldValue(p) > bestoldval)
                    {
                        bestoldval = botBase.getPlayfieldValue(p);
                        bestold = p;
                    }
                    if (!test)
                    {
                        posmoves.Remove(p);
                    }

                }

                if (!test && bestoldval >= -10000 && bestold != null)
                {
                    this.posmoves.Add(bestold);
                }

                help.loggonoff(true);
                int donec = 0;
                foreach (Playfield p in posmoves)
                {
                    if (p.complete) donec++;
                }
                help.logg("deep " + deep + " len " + this.posmoves.Count + " dones " + donec);

                if (!test)
                {
                    cuttingposibilities(botBase);
                }

                /*if ((deep + 1) % 4 == 0)
                {
                    help.logg("cut");
                }*/
                help.loggonoff(false);
                deep++;

                if (deep >= 20) break;//remove this?
            }

            int bestval = -10;
            int bestanzactions = 1000;
            Playfield bestplay = temp[0];
            foreach (Playfield p in temp)
            {
                int val = botBase.getPlayfieldValue(p);
                if (bestval <= val)
                {
                    if (bestval == val && bestanzactions < p.playactions.Count) continue;
                    bestplay = p;
                    bestval = val;
                    bestanzactions = p.playactions.Count;
                }

            }
            help.loggonoff(true);
            help.logg("############################################");
            help.logg("bestPlayvalue " + bestval);

            if (!test)
            {
                bestplay.doAction();
            }
            bestplay.printActions();
            this.bestmove = bestplay.getNextAction();
        }


        private void cuttingposibilities(BotBase botBase)
        {
            // take the x best values
            int takenumber = 1000;
            List<Playfield> temp = new List<Playfield>();
            posmoves.Sort((a, b) => -(botBase.getPlayfieldValue(a)).CompareTo(botBase.getPlayfieldValue(b)));//want to keep the best
            temp.AddRange(posmoves);
            posmoves.Clear();
            posmoves.AddRange(Helpfunctions.TakeList(temp, takenumber));

        }



        public void dosomethingclever(BotBase botbase)
        {
            //return;
            //turncheck
            //help.moveMouse(950,750);
            //help.Screenshot();
            posmoves.Clear();
            posmoves.Add(new Playfield());

           /* foreach (var item in this.posmoves[0].owncards)
            {
                help.logg("card " + item.card.name + " is playable :" + item.card.canplayCard(posmoves[0]) + " cost/mana: " + item.card.cost + "/" + posmoves[0].mana);
            }
            */
            //help.logg("is hero ready?" + posmoves[0].ownHeroReady);

            help.loggonoff(false);
            doallmoves(false, botbase);
            //help.logging(true);

        }

        public void simulatorTester(BotBase botbase)
        {
            //setup cards in hand
            this.hm.loadPreparedBattlefield(3);

            //setup minions on field, hero hp, weapons and stuff
            this.hp.loadPreparedBattlefield(0);

            //calculate the stuff
            posmoves.Clear();
            posmoves.Add(new Playfield());
            help.logg("ownminionscount " + posmoves[0].ownMinions.Count);
            help.logg("owncardscount " + posmoves[0].owncards.Count);

            foreach (var item in this.posmoves[0].owncards)
            {
                help.logg("card " + item.card.name + " is playable :" + item.card.canplayCard(posmoves[0]) +" cost/mana: " + item.card.cost +"/"+ posmoves[0].mana);
            }

            doallmoves(true, botbase);
            foreach (Playfield p in this.posmoves)
            {
                p.printBoard();
            }
        }

    }


}
