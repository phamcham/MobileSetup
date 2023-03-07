using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhamCham.Firebase.Leaderboard {
    public struct User {
        public long score;
        public Dictionary<string, object> data;

        public T GetDataOrDefault<T>(string key, T defaultValue) {
            if (data.ContainsKey(key)) {
                try {
                    T value = (T)data[key];
                    return value;
                }
                catch { }
            }

            return defaultValue;
        }
    }

    public struct UserIndex {
        public string key;
        public User value;

        public bool Same(UserIndex other) {
            if (key != other.key) {
                return false;
            }

            if (value.score != other.value.score) {
                return false;
            }

            foreach (var o in value.data) {
                if (!other.value.data.ContainsKey(o.Key) || other.value.data[o.Key] != o.Value) {
                    return false;
                }
            }

            foreach (var o in other.value.data) {
                if (!value.data.ContainsKey(o.Key) || value.data[o.Key] != o.Value) {
                    return false;
                }
            }

            return true;
        }
    }
}