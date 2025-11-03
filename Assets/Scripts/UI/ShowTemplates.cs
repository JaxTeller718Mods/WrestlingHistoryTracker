using System;
using System.Collections.Generic;

public static class ShowTemplates
{
    public class Template
    {
        public string name;
        public int matches;
        public bool openerPromo;
        public bool midPromo;
    }

    public static readonly List<Template> Templates = new()
    {
        new Template { name = "Weekly TV", matches = 5, openerPromo = true, midPromo = true },
        new Template { name = "PPV", matches = 8, openerPromo = true, midPromo = true },
        new Template { name = "House Show", matches = 6, openerPromo = false, midPromo = false }
    };
}

