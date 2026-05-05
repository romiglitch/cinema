using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shipping
{
    // מחלקת האב (Base Class) - מגדירה את הבסיס לכל סרט במערכת
    public class Movie
    {
        // תכונות משותפות לכל סוגי הסרטים
        public int Id { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public int Dur { get; set; }
        public int Age { get; set; }
        public string Poster { get; set; }
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public string TrailerUrl { get; set; }

        // בנאי ברירת מחדל
        public Movie() { }

        // בנאי מלא לאתחול כל פרטי הסרט
        public Movie(int id, string title, string desc, int dur, int age, string poster)
        {
            Id = id;
            Title = title;
            Desc = desc;
            Dur = dur;
            Age = age;
            Poster = poster;
        }

        // בנאי מקוצר (למשל לתצוגה מקדימה ברשימת סרטים)
        public Movie(int id, string title, string poster)
        {
            this.Id = id;
            this.Title = title;
            this.Poster = poster;
        }
    }

    //סרט עם תוספות של בית קולנוע :Movieיורשת מ CinemaMovie
    public class CinemaMovie : Movie
    {
        // תכונות שקיימות רק בסרט שמוקרן באולם קולנוע פיזי
        public double TicketPrice { get; set; }
        public string CinemaHall { get; set; }

        // פונקציה להצגת אורך הסרט
        public string GetFormattedDuration() => $"{Dur} דקות של חוויה";

        // : base() בנאי ריק שקורא לבנאי הריק של האב בעזרת
        public CinemaMovie() : base() { }

        //בנאי שמשתמש בירושה
        // ורק המחיר נשמר בבן Movieהפרמטרים נשלחים לבנאי של ה
        public CinemaMovie(int id, string title, string desc, int dur, int age, string poster, double price)
            : base(id, title, desc, dur, age, poster)
        {
            this.TicketPrice = price;
        }

        // מתודה ייחודית שקיימת רק בבן - מאפשרת להציג את המחיר עם סימן שקל
        public string GetDisplayPrice()
        {
            return this.TicketPrice + " ₪";
        }
    }
}
