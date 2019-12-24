package models

import "github.com/jinzhu/gorm"

type Cuntomer struct {
	gorm.Model
	Name string
}
