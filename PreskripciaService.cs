.FirstOrDefaultAsync(l => l.Pouzivatel.JePrimatel == true && l.Pouzivatel.TokenPrimatela == tokenPrimatela);
