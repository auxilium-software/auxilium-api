from typing import List

from pydantic import BaseModel, Field


class SurveyResultsModel(BaseModel):
    name_of_organisation:       str
    start_date:                 str
    drop_in_location:           str

    year_of_birth:              str
    gender:                     str
    employed:                   str

    issues_that_apply:          List[str] = Field(default_factory=list, description="List of issues that apply to the veteran")

    when_issues_started:        str
    had_legal_advice:           str

    knew_about_legal_issues:    str
    which_didnt_know:           str

    wants_legal_charity_help:   str
