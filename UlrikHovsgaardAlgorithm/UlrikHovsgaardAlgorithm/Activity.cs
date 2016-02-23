﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class Activity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Included { get; set; }
        public bool Executed { get; set; }
        public bool Pending { get; set; }
        public List<string> Roles { get; set; }

        public override bool Equals(object obj)
        {
            Activity otherActivity = obj as Activity;
            if (otherActivity != null)
            {
                return this.Id.Equals(otherActivity.Id);
            }
            else
            {
                throw new ArgumentException();
            }

        }
    }
}