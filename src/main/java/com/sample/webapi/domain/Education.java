package com.sample.webapi.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Table;

/** Education record. Table and column names match the schema created by the original EF Core migration. */
@Entity
@Table(name = "Educations")
public class Education extends EntityBase {

    @Column(name = "Degree", length = 50, nullable = false)
    private String degree;

    @Column(name = "FieldOfStudy", length = 250, nullable = false)
    private String fieldOfStudy;

    @Column(name = "School", length = 250, nullable = false)
    private String school;

    @Column(name = "Description", length = 1000)
    private String description;

    public String getDegree() {
        return degree;
    }

    public void setDegree(String degree) {
        this.degree = degree;
    }

    public String getFieldOfStudy() {
        return fieldOfStudy;
    }

    public void setFieldOfStudy(String fieldOfStudy) {
        this.fieldOfStudy = fieldOfStudy;
    }

    public String getSchool() {
        return school;
    }

    public void setSchool(String school) {
        this.school = school;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }
}
