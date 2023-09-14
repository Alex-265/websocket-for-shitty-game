using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Jeopardy_Websocket {

    public class DataManager {

        

        private List<User> users = new List<User>();
        private List<Question> questions = new List<Question>();

        public void AddUser(User user) {
            users.Add(user);
        }

        public List<User> GetAllUsers() {
            return users;
        }

        public User GetUser(string name) {
            return users.FirstOrDefault(u => u.Name == name);
        }

        public void AddQuestion(Question question) {
            questions.Add(question);
        }

        public Question GetQuestion(string category, int number) {
            return questions.FirstOrDefault(q => q.Category == category && q.Number == number);
        }
        public void SaveUsers(string fileName) {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(users));
        }

        public void LoadUsers(string fileName) {
            if (File.Exists(fileName)) {
                string json = File.ReadAllText(fileName);
                users = JsonConvert.DeserializeObject<List<User>>(json);
            }
        }



        public void SaveQuestions(string fileName) {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(questions));
        }

        public void LoadQuestions(string fileName) {
            if (File.Exists(fileName)) {
                string json = File.ReadAllText(fileName);
                questions = JsonConvert.DeserializeObject<List<Question>>(json);
            }
        }
    }

}
