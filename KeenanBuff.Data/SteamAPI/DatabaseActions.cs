﻿using System;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Collections.Generic;
using KeenanBuff.Data.SteamAPI.APIModels;
using KeenanBuff.Data.Context;
using KeenanBuff.Entities;

namespace KeenanBuff.Data.SteamAPI
{
    internal class DatabaseActions //: IDatabaseActions
    {


        public void Update(ApplicationDbContext context)
        {

            if (System.Diagnostics.Debugger.IsAttached == false)
            {

               System.Diagnostics.Debugger.Launch();

            }     

            var Matches = new List<Entities.Match>();


            //convert the API models into the ViewModels
            var heroes = APIcalls.getHeroes().Select(x => new Entities.Hero()
            {
                HeroId = x.id,
                HeroName = x.localized_name,
                HeroUrl = GetHeroURL(x.name),
            }).ToList();

            var gameItems = APIcalls.getItems().Select(x => new Entities.Item()
            {
                ItemId = x.id,
                Name = x.localized_name,
                ItemUrl = "http://cdn.dota2.com/apps/dota2/images/items/" + x.name.Remove(0,5) + "_lg.png"
            }).ToList();

            gameItems.Add(new Entities.Item { ItemId = 0, Name = "Empty", ItemUrl = "None" });

            //add to the db
            gameItems.ForEach(s => context.Items.AddOrUpdate(s));
            heroes.ForEach(s => context.Heroes.AddOrUpdate(s));




            //Write to match details table. This uses the matchIDs from above, 
            //itterates through them, grabs the details and creates a match
            var account_id = 90935174; //this is my account!
            foreach (var game in APIcalls.getMatchHistory(account_id).matches)
            {
                var apiMatchDetails = APIcalls.getMatchDetails(game.match_id);

                //if the game is already there, skip
                if (context.Matches.Any(m => m.MatchID == game.match_id))
                {
                    continue;
                }

                var MatchDetails = new List<MatchDetail>();

                foreach (var item in apiMatchDetails.result.players)
                {

                    var PlayerItems = new List<Entities.PlayerItem>() {
                        new PlayerItem {
                            ItemId = item.item_0
                        }, 
                        new PlayerItem {
                            ItemId = item.item_1,
                        },
                        new PlayerItem {
                            ItemId = item.item_2,
                        },
                        new PlayerItem {
                            ItemId = item.item_3,
                        },
                        new PlayerItem {
                            ItemId = item.item_4,
                        },
                        new PlayerItem {
                            ItemId = item.item_5,
                        }};

                    var MatchDetail = new MatchDetail
                    {
                        PlayerID = item.account_id,
                        MatchID = game.match_id,
                        PlayerSlot = item.player_slot,
                        HeroId = item.hero_id,
                        PlayerItems = PlayerItems,
                        Kills = item.kills,
                        Deaths = item.deaths,
                        Assists = item.assists,
                        LeaverStatus = item.leaver_status,
                        LastHits = item.last_hits,
                        Denies = item.denies,
                        GoldPerMin = item.gold_per_min,
                        XpPerMin = item.xp_per_min,
                        Level = item.level,
                        Gold = item.gold,
                        GoldSpent = item.gold_spent,
                        HeroDamage = item.hero_damage,
                        HeroHealing = item.hero_healing,
                        TowerDamage = item.tower_damage,
                        SteamName = item.steam_name
                    };

                    MatchDetails.Add(MatchDetail);
                }

                var match = new Entities.Match
                {
                    MatchID = game.match_id,
                    MatchSeqNum = game.match_id, //I don't really know why I need this but I have it just incase
                    StartTime = UnixTimeStampToDateTime(apiMatchDetails.result.start_time),
                    LobbyType = apiMatchDetails.result.lobby_type,
                    LobbyString = getLobbyString(apiMatchDetails.result.lobby_type),
                    RadiantWin = apiMatchDetails.result.radiant_win,
                    GameMode = apiMatchDetails.result.game_mode,
                    GameModeString = getGameMode(apiMatchDetails.result.game_mode),
                    Duration = apiMatchDetails.result.duration,
                    TowerStatusDire = apiMatchDetails.result.tower_status_dire,
                    TowerStatusRadiant = apiMatchDetails.result.tower_status_radiant,
                    BarracksStatusDire = apiMatchDetails.result.tower_status_dire,
                    BarracksStatusRadiant = apiMatchDetails.result.barracks_status_radiant,
                    FirstBloodTime = apiMatchDetails.result.first_blood_time,
                    RadiantScore = apiMatchDetails.result.radiant_score,
                    DireScore = apiMatchDetails.result.dire_score,
                    MatchDetails = MatchDetails
                };
        
                MatchDetails.ForEach(s => context.MatchDetails.AddOrUpdate(s));
                Matches.Add(match);
                //onto the next game!
            }

            Matches.ForEach(s => context.Matches.AddOrUpdate(s));
        }


        //Helper classes
        private string getLobbyString(int lobby_type) {
            Dictionary<int, string> lobbyString = new Dictionary<int, string>()
            {
                {-1, "Invalid"},
                {0, "Normal"},
                {1, "Practice"},
                {2, "Tournament"},
                {3, "Tutorial"},
                {4, "Co-op"},
                {5, "Team"},
                {6, "Solo"},
                {7, "Ranked"},
                {8, "1 v 1"}
            };

            if (lobbyString.TryGetValue(lobby_type, out string value))
            {
                return value;
            } else
            {
                return "Invalid";
            }

         }

        private string getGameMode(int game_mode)
        {
            Dictionary<int, string> gameString = new Dictionary<int, string>()
            {
                {-1, "Invalid"},
                {0, "None"},
                {1, "All Pick"},
                {2, "Captain's Mode"},
                {3, "Random Draft"},
                {4, "Single Draft"},
                {5, "All Random"},
                {6, "Intro"},
                {7, "Diretide"},
                {8, "Reverse Captain's Mode"},
                {9, "The Greeviling"},
                {10, "Tutorial"},
                {11, "Mid Only"},
                {12, "Least Played"},
                {13, "New Player Pool"},
                {14, "Compendium Matchmaking"},
                {15, "Co-op vs Bots"},
                {16, "Captains Draft"},
                {18, "Ability Draft"},
                {20, "All Random Deathmatch"},
                {21, "1v1 Mid Only"},
                {22, "Ranked Matchmaking"}
            };

            if (gameString.TryGetValue(game_mode, out string value))
            {
                return value;
            }
            else
            {
                return "Invalid";
            }

        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            try
            {
                System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
                return dtDateTime;
            }
            catch (Exception)
            {

                return DateTime.Today;
            }

        }

        private static string GetHeroURL(string name)
        {
            return "http://cdn.dota2.com/apps/dota2/images/heroes/" + name.Remove(0, 14) +"_sb.png";
        }


    }


}
