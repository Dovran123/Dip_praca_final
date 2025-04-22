.Where(p => (p.PO as List<string>)?.Count > 0 && p.PO != null && !(p.PO as List<string>)!.Contains(kodOdbornosti))
