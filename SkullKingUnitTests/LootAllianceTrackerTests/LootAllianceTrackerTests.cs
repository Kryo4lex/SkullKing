using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;

namespace SkullKingUnitTests.LootAllianceTrackerTests
{
    using System.Collections.Generic;
    using Xunit;

    using SkullKingCore.Core.Cards;
    using SkullKingCore.Core.Cards.Base;
    using SkullKingCore.Core.Cards.Implementations;
    using SkullKingCore.Core.Cards.SubCardTypes;

    using SkullKingCore.Core.Game;

    namespace SkullKingUnitTests.BonusPointsTests
    {
        public class LootAllianceTrackerTests
        {
            // -------- helpers (strict) --------

            private static GameState MakeStateWithPlayers(int playerCount, int roundNumber)
            {
                var players = new List<Player>();
                for (int i = 0; i < playerCount; i++)
                {
                    players.Add(new Player("P" + i, "Player " + i));
                    // ensure a RoundStat exists for the round
                    players[i].RoundStats.Add(new RoundStat(roundNumber, 0));
                }

                var state = new GameState(players, roundNumber, roundNumber, new List<Card>());
                state.StartingPlayerIndex = 0;
                return state;
            }

            private static RoundStat GetStatNonNull(GameState state, int absPlayerIndex, int round)
            {
                var list = state.Players[absPlayerIndex].RoundStats;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Round == round) return list[i];
                }
                // strict: if not found, fail the test clearly
                throw new System.InvalidOperationException($"RoundStat for round {round} not found for player {absPlayerIndex}.");
            }

            private static void SetBidAndActual(GameState state, int absPlayerIndex, int round, int predicted, int actual)
            {
                var rs = GetStatNonNull(state, absPlayerIndex, round);
                rs.PredictedWins = predicted;
                rs.ActualWins = actual;
            }

            // -------- tests --------

            [Fact]
            public void Loot_AlliesWithWinner_BothExact_GetPlus20Each()
            {
                int round = 3;
                GameState state = MakeStateWithPlayers(3, round);
                LootAllianceTracker tracker = new LootAllianceTracker();
                tracker.Reset();

                var trick = new List<Card>
            {
                new LootCard(),                          // P0
                new NumberCard(CardType.YELLOW, 10),     // P1 (winner)
                new NumberCard(CardType.GREEN, 9),       // P2
            };

                int? winnerRelIndex = 1; // P1 wins
                int startingPlayerAbsIndex = 0;
                int playerCount = 3;

                SetBidAndActual(state, 0, round, 1, 1); // P0 exact
                SetBidAndActual(state, 1, round, 2, 2); // P1 exact
                SetBidAndActual(state, 2, round, 0, 1); // P2 miss

                tracker.ObserveTrick(trick, winnerRelIndex, startingPlayerAbsIndex, playerCount);
                tracker.ApplyEndOfRoundBonuses(state, round);

                int p0Bonus = GetStatNonNull(state, 0, round).BonusPoints;
                int p1Bonus = GetStatNonNull(state, 1, round).BonusPoints;
                int p2Bonus = GetStatNonNull(state, 2, round).BonusPoints;

                Assert.Equal(20, p0Bonus);
                Assert.Equal(20, p1Bonus);
                Assert.Equal(0, p2Bonus);
            }

            [Fact]
            public void Loot_AlliesWithWinner_OnlyLootExact_NoBonus()
            {
                int round = 4;
                GameState state = MakeStateWithPlayers(3, round);
                LootAllianceTracker tracker = new LootAllianceTracker();
                tracker.Reset();

                var trick = new List<Card>
            {
                new LootCard(),                        // P0
                new NumberCard(CardType.GREEN, 12),    // P1 (winner)
                new NumberCard(CardType.YELLOW, 2),    // P2
            };

                int? winnerRelIndex = 1;
                int startingPlayerAbsIndex = 0;
                int playerCount = 3;

                SetBidAndActual(state, 0, round, 1, 1); // exact
                SetBidAndActual(state, 1, round, 2, 1); // miss
                SetBidAndActual(state, 2, round, 0, 0); // irrelevant

                tracker.ObserveTrick(trick, winnerRelIndex, startingPlayerAbsIndex, playerCount);
                tracker.ApplyEndOfRoundBonuses(state, round);

                int p0Bonus = GetStatNonNull(state, 0, round).BonusPoints;
                int p1Bonus = GetStatNonNull(state, 1, round).BonusPoints;

                Assert.Equal(0, p0Bonus);
                Assert.Equal(0, p1Bonus);
            }

            [Fact]
            public void Loot_CaptainsLog_LootWins_NoAlliance()
            {
                int round = 5;
                GameState state = MakeStateWithPlayers(3, round);
                LootAllianceTracker tracker = new LootAllianceTracker();
                tracker.Reset();

                var trick = new List<Card>
            {
                new LootCard(),     // P0 (winner in all-escape-like case)
                new EscapeCard(),   // P1
                new EscapeCard(),   // P2
            };

                int? winnerRelIndex = 0; // P0 wins
                int startingPlayerAbsIndex = 0;
                int playerCount = 3;

                SetBidAndActual(state, 0, round, 1, 1);
                SetBidAndActual(state, 1, round, 1, 1);
                SetBidAndActual(state, 2, round, 1, 1);

                tracker.ObserveTrick(trick, winnerRelIndex, startingPlayerAbsIndex, playerCount);
                tracker.ApplyEndOfRoundBonuses(state, round);

                int p0Bonus = GetStatNonNull(state, 0, round).BonusPoints;
                int p1Bonus = GetStatNonNull(state, 1, round).BonusPoints;
                int p2Bonus = GetStatNonNull(state, 2, round).BonusPoints;

                Assert.Equal(0, p0Bonus);
                Assert.Equal(0, p1Bonus);
                Assert.Equal(0, p2Bonus);
            }

            [Fact]
            public void Loot_Kraken_Cancels_NoAlliance()
            {
                int round = 6;
                GameState state = MakeStateWithPlayers(3, round);
                LootAllianceTracker tracker = new LootAllianceTracker();
                tracker.Reset();

                var trick = new List<Card>
            {
                new LootCard(),   // P0
                new KrakenCard(), // P1
            };

                int? winnerRelIndex = null;  // cancelled trick
                int startingPlayerAbsIndex = 0;
                int playerCount = 3;

                SetBidAndActual(state, 0, round, 0, 0);
                SetBidAndActual(state, 1, round, 1, 1);
                SetBidAndActual(state, 2, round, 0, 0);

                tracker.ObserveTrick(trick, winnerRelIndex, startingPlayerAbsIndex, playerCount);
                tracker.ApplyEndOfRoundBonuses(state, round);

                int p0Bonus = GetStatNonNull(state, 0, round).BonusPoints;
                int p1Bonus = GetStatNonNull(state, 1, round).BonusPoints;

                Assert.Equal(0, p0Bonus);
                Assert.Equal(0, p1Bonus);
            }

            [Fact]
            public void Loot_TwoLoots_SameWinner_AllExact_WinnerGets40_EachLootGets20()
            {
                int round = 7;
                GameState state = MakeStateWithPlayers(4, round);
                LootAllianceTracker tracker = new LootAllianceTracker();
                tracker.Reset();

                var trick = new List<Card>
            {
                new LootCard(),                          // P0 loot
                new LootCard(),                          // P1 loot
                new NumberCard(CardType.YELLOW, 12),     // P2
                new NumberCard(CardType.YELLOW, 13),     // P3 winner
            };

                int? winnerRelIndex = 3;
                int startingPlayerAbsIndex = 0;
                int playerCount = 4;

                SetBidAndActual(state, 0, round, 1, 1);
                SetBidAndActual(state, 1, round, 2, 2);
                SetBidAndActual(state, 2, round, 0, 0);
                SetBidAndActual(state, 3, round, 3, 3);

                tracker.ObserveTrick(trick, winnerRelIndex, startingPlayerAbsIndex, playerCount);
                tracker.ApplyEndOfRoundBonuses(state, round);

                int p0Bonus = GetStatNonNull(state, 0, round).BonusPoints; // loot #1
                int p1Bonus = GetStatNonNull(state, 1, round).BonusPoints; // loot #2
                int p2Bonus = GetStatNonNull(state, 2, round).BonusPoints; // uninvolved
                int p3Bonus = GetStatNonNull(state, 3, round).BonusPoints; // winner

                Assert.Equal(20, p0Bonus);
                Assert.Equal(20, p1Bonus);
                Assert.Equal(0, p2Bonus);
                Assert.Equal(40, p3Bonus);
            }
        }
    }
}