﻿using System;
using System.Collections.Generic;

namespace OSL.DAL.Entities;

public partial class Question
{
    public long QuestionId { get; set; }

    public string Topic { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Explanation { get; set; } = null!;

    public long? UserId { get; set; }

    public virtual User? User { get; set; }
}