using GazeStream.Eyetracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GazeStream.AppData
{
    public class User
    {
        public string name;
        public string email;
        public Guid ID { get; private set; }
        public GazeSettings Gaze { get; private set; }

        public string UniqueFolderName => $"{name}_{ID.ToString()}";
        public User(string name, string email)
        {
            this.name = name;
            this.email = email;
            ID = Guid.NewGuid();
            Gaze = new GazeSettings();
        }

        public static User GetDefaultUser()
        {
            User defaultUser = new User("Default", "default@example.com");
            defaultUser.ID = Guid.Parse("00000000-0000-0000-0000-000000000001");
            return defaultUser;
        }
    }
}
