using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shipping
{
    // מחלקת האב (Base Class)
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public int Dur { get; set; }
        public int Age { get; set; }
        public string Poster { get; set; }
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public string TrailerUrl { get; set; }

        public Movie() { }

        public Movie(int id, string title, string desc, int dur, int age, string poster)
        {
            Id = id;
            Title = title;
            Desc = desc;
            Dur = dur;
            Age = age;
            Poster = poster;
        }

        public Movie(int id, string title, string poster)
        {
            this.Id = id;
            this.Title = title;
            this.Poster = poster;
        }
    }

    // --- כאן נכנסת הירושה ---
    // מחלקת הבן (Derived Class)
    public class CinemaMovie : Movie
    {
        public string GetFormattedDuration() => $"{Dur} דקות של חוויה";
        public double TicketPrice { get; set; }
        public string CinemaHall { get; set; }

        // בנאי ריק שקורא לבנאי הריק של האב
        public CinemaMovie() : base() { }

        // בנאי מלא שמעביר את הפרטים לאב (base) ומוסיף מחיר
        public CinemaMovie(int id, string title, string desc, int dur, int age, string poster, double price)
            : base(id, title, desc, dur, age, poster)
        {
            this.TicketPrice = price;
        }

        // מתודה ייחודית לבן - מראה פולימורפיזם/הרחבה
        public string GetDisplayPrice()
        {
            return this.TicketPrice + " ₪";
        }
    }
}