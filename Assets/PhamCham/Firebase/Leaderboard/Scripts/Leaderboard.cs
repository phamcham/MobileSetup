using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using UnityEngine;

namespace PhamCham.Firebase.Leaderboard {
    public class Leaderboard {
        private List<UserIndex> cache_users;
        UserIndex playerUser;
        DatabaseReference reference;
        string leaderboardName;

        public bool IsLoading { get; private set; }

        const string ROOT = "leaderboard v2";
        const string SCORE = "score";

        public Leaderboard(DatabaseReference reference, string leaderboardName) {
            this.reference = reference;
            this.leaderboardName = leaderboardName;
        }

        public void LoadLeaderboard(UserIndex playerUser, Action<UserIndex, List<UserIndex>> callback, int limit = 100) {
            if (IsLoading) {
                Debug.LogWarning(leaderboardName + " is loading!, ignore callback!");
            }

            if (playerUser.Same(this.playerUser)) {
                callback?.Invoke(playerUser, cache_users);
                return;
            }

            this.playerUser = playerUser;

            IsLoading = true;

            reference.Child(ROOT).Child(leaderboardName).OrderByChild(SCORE).LimitToLast(limit)
            .GetValueAsync().ContinueWithOnMainThread(task => {
                IsLoading = false;

                LoadLeaderboardCompletedHandle(task, playerUser, callback);
            })
            .LogExceptionIfFaulted();

        }

        private void LoadLeaderboardCompletedHandle(Task<DataSnapshot> task, UserIndex playerUser, Action<UserIndex, List<UserIndex>> callback) {
            if (!task.IsCompleted) {
                callback?.Invoke(playerUser, null);
                return;
            }

            DataSnapshot snapshot = task.Result;
            if (snapshot == null) {
                Debug.Log("snapshot is null");
                callback?.Invoke(playerUser, new());
                return;
            }

            string json = snapshot.GetRawJsonValue();

            try {
                // TODO: loi monthly xay ra tai day, can tim hieu
                Dictionary<string, User> dict = JsonConvert.DeserializeObject<Dictionary<string, User>>(json);

                if (dict.ContainsKey(playerUser.key)) {
                    // neu nguoi dung co trong bxh thi thay the local chu k cap nhat
                    dict[playerUser.key] = playerUser.value;
                }

                cache_users = dict.Select(x => new UserIndex() { key = x.Key, value = x.Value }).ToList();

                cache_users.Sort((a, b) => b.value.score.CompareTo(a.value.score));

                if (cache_users.Count > 0 && playerUser.value.score >= cache_users[^1].value.score) {
                    // neu diem so lon hon nguoi cuoi cung thi them nguoi choi vao
                    cache_users.Add(playerUser);
                    cache_users.Sort((a, b) => b.value.score.CompareTo(a.value.score));
                }


                callback?.Invoke(playerUser, cache_users);

                Debug.Log("log player to " + leaderboardName);
                // cap nhat thong tin user sau
                PostPlayerData(playerUser);
            }
            catch {
                callback?.Invoke(playerUser, new() { new UserIndex() { key = "something wrong???", value = new() { score = 1, data = new() { ["username"] = "???", ["premium"] = false } } } });
            }
        }

        public void PostPlayerData(UserIndex playerUser) {
            try {
                string json = JsonConvert.SerializeObject(playerUser.value);

                reference.Child(ROOT).Child(leaderboardName).Child(playerUser.key)
                .SetRawJsonValueAsync(json)
                .LogExceptionIfFaulted();
            }
            catch { }
        }
    }
}