
import { useEffect, useRef, useState } from "react";
import { Oval } from 'react-loader-spinner'
import { useDispatch, useSelector } from 'react-redux';
import { Alert } from "reactstrap";
import { loadLidarrProfiles as loadProfiles } from "../../../store/actions/LidarrClientActions"
import { loadLidarrMetadataProfiles as loadMetadataProfiles } from "../../../store/actions/LidarrClientActions"
import { loadLidarrRootPaths as loadRootPaths } from "../../../store/actions/LidarrClientActions"
import { loadLidarrTags as loadTags } from "../../../store/actions/LidarrClientActions"
import { setLidarrCategory } from "../../../store/actions/LidarrClientActions"
import { removeLidarrCategory } from "../../../store/actions/LidarrClientActions"
import ValidatedTextbox from "../../Inputs/ValidatedTextbox"
import Dropdown from "../../Inputs/Dropdown"
import MultiDropdown from "../../Inputs/MultiDropdown"

import {
  Row,
  Col,
  Collapse
} from "reactstrap";


function LidarrCategory(props) {
  const [nameErrorMessage, setNameErrorMessage] = useState("");
  const [isNameValid, setIsNameValid] = useState(true);
  const [isOpen, setIsOpen] = useState(false);

  const propRef = useRef();

  const reduxState = useSelector((state) => {
    return {
      lidarr: state.music.lidarr,
      otherCategories: state.music.otherCategories
    }
  });
  const dispatch = useDispatch();


  useEffect(() => {
    if (props.category.wasCreated)
      setIsOpen(true);

    if (props.canConnect) {
      dispatch(loadProfiles(false));
      dispatch(loadMetadataProfiles(false));
      dispatch(loadRootPaths(false));
      dispatch(loadTags(false));
    }
  }, []);


  useEffect(() => {
    const prevState = propRef.pastState;
    propRef.pastState = reduxState;
    const prevProps = propRef.past;
    propRef.past = props;

    let previousNames = prevState === undefined ? [] : prevState.lidarr.categories.map(x => x.name);
    let currentNames = reduxState.lidarr.categories.map(x => x.name);

    if (!(previousNames.length === currentNames.length && currentNames.every((value, index) => previousNames[index] === value)))
      validateName(props.category.name)

    if (props.canConnect) {
      dispatch(loadProfiles(false));
      dispatch(loadMetadataProfiles(false));
      dispatch(loadRootPaths(false));
      dispatch(loadTags(false));
    }

    if (prevProps?.isSaving !== props.isSaving)
      setIsOpen(false)
  });




  // const validateNonEmptyString = (value) => {
  //   return /\S/.test(value);
  // }

  const validateName = (value) => {
    let newIsNameValid = true;
    let newNameErrorMessage = undefined;

    if (!/\S/.test(value)) {
      newNameErrorMessage = "A category name is required.";
      newIsNameValid = false;
    } else if (/^[\w-]{1,32}$/.test(value)) {
      if (reduxState.lidarr.categories.map(x => x.id).includes(props.category.id) && reduxState.lidarr.categories.filter(c => typeof c.id !== 'undefined' && c.id !== props.category.id && c.name.toLowerCase().trim() === value.toLowerCase().trim()).length > 0) {
        newNameErrorMessage = "All categories must have different names.";
        newIsNameValid = false;
      } else if (reduxState.otherCategories.filter(c => c.toLowerCase().trim() === value.toLowerCase().trim()).length > 0) {
        newNameErrorMessage = "This category has matched a category in Movies or TV Shows, this must have different names.";
        newIsNameValid = false;
      }
    } else {
      newNameErrorMessage = "Invalid categorie names, make sure they only contain alphanumeric characters, dashes and underscores. (No spaces, etc)";
      newIsNameValid = false;
    }

    setIsNameValid(newIsNameValid);
    if (newNameErrorMessage !== undefined)
      setNameErrorMessage(newNameErrorMessage);

    return newIsNameValid;
  };




  const setCategory = (fieldChanged, data) => {
    dispatch(setLidarrCategory(props.category.id, fieldChanged, data));
  };


  const deleteCategory = () => {
    setIsOpen(false);
    setTimeout(() => dispatch(removeLidarrCategory(props.category.id)), 150);
  };




  return (
    <>
      <tr class="fade show">
        <th scope="row">
          <div class="media align-items-center">
            <div class="media-body">
              <span class="name mb-0 text-sm">{props.category.name}</span>
            </div>
          </div>
        </th>
        <td class="text-right">
          <button onClick={() => setIsOpen(!isOpen)} disabled={isOpen && (!isNameValid || !reduxState.lidarr.arePathsValid || !reduxState.lidarr.areProfilesValid || !reduxState.lidarr.areTagsValid)} className="btn btn-icon btn-3 btn-info" type="button">
            <span className="btn-inner--icon"><i class="fas fa-edit"></i></span>
            <span className="btn-inner--text">Edit</span>
          </button>
        </td>
      </tr>
      <tr>
        <td className="border-0 pb-0 pt-0" colSpan="2">
          <Collapse isOpen={isOpen}>
            <div className="mb-4">
              <Row>
                <Col lg="6">
                  <ValidatedTextbox
                    name="Category Name"
                    placeholder="Enter category name"
                    alertClassName="mt-3 text-wrap"
                    errorMessage={nameErrorMessage}
                    isSubmitted={props.isSubmitted || isNameValid}
                    value={props.category.name}
                    validation={validateName}
                    onChange={newName => setCategory("name", newName)}
                    onValidate={isValid => setIsNameValid(isValid)} />
                </Col>
              </Row>
              <Row>
                <Col lg="6" className="mb-4">
                  <div className="input-group-button">
                    <Dropdown
                      name="Path"
                      value={props.category.rootFolder}
                      items={reduxState.lidarr.paths.map(x => { return { name: x.path, value: x.path } })}
                      onChange={newPath => setCategory("rootFolder", newPath)} />
                    <button className="btn btn-icon btn-3 btn-default" onClick={() => dispatch(loadRootPaths(true))} disabled={!props.canConnect} type="button">
                      <span className="btn-inner--icon">
                        {
                          reduxState.lidarr.isLoadingPaths ? (
                            <Oval
                              wrapperClass="loader"
                              type="Oval"
                              color="#11cdef"
                              height={19}
                              width={19}
                            />)
                            : (<i className="fas fa-download"></i>)
                        }</span>
                      <span className="btn-inner--text">Load</span>
                    </button>
                  </div>
                  {
                    !reduxState.lidarr.arePathsValid ? (
                      <Alert className="mt-3 mb-0 text-wrap " color="warning">
                        <strong>Could not find any paths.</strong>
                      </Alert>)
                      : null
                  }
                  {
                    props.isSubmitted && reduxState.lidarr.paths.length === 0 ? (
                      <Alert className="mt-3 mb-0 text-wrap " color="warning">
                        <strong>A path is required.</strong>
                      </Alert>)
                      : null
                  }
                </Col>
                <Col lg="6" className="mb-4">
                  <div className="input-group-button">
                    <Dropdown
                      name="Profile"
                      value={props.category.profileId}
                      items={reduxState.lidarr.profiles.map(x => { return { name: x.name, value: x.id } })}
                      onChange={newProfile => setCategory("profileId", newProfile)} />
                    <button className="btn btn-icon btn-3 btn-default" onClick={() => dispatch(loadProfiles(true))} disabled={!props.canConnect} type="button">
                      <span className="btn-inner--icon">
                        {
                          reduxState.lidarr.isLoadingProfiles ? (
                            <Oval
                              wrapperClass="loader"
                              type="Oval"
                              color="#11cdef"
                              height={19}
                              width={19}
                            />)
                            : (<i className="fas fa-download"></i>)
                        }</span>
                      <span className="btn-inner--text">Load</span>
                    </button>
                  </div>
                  {
                    !reduxState.lidarr.areProfilesValid ? (
                      <Alert className="mt-3 mb-0 text-wrap " color="warning">
                        <strong>Could not find any profiles.</strong>
                      </Alert>)
                      : null
                  }
                  {
                    props.isSubmitted && reduxState.lidarr.profiles.length === 0 ? (
                      <Alert className="mt-3 mb-0 text-wrap " color="warning">
                        <strong>A profile is required.</strong>
                      </Alert>)
                      : null
                  }
                </Col>
              </Row>
              <Row>
                <Col lg="6">
                  <div className="input-group-button mb-4">
                    <MultiDropdown
                      name="Tags"
                      placeholder=""
                      labelField="name"
                      valueField="id"
                      ignoreEmptyItems={true}
                      selectedItems={reduxState.lidarr.tags.filter(x => props.category.tags.includes(x.id))}
                      items={reduxState.lidarr.tags}
                      onChange={newTags => setCategory("tags", newTags.map(x => x.id))} />
                    <button className="btn btn-icon btn-3 btn-default" onClick={() => dispatch(loadTags(true))} disabled={!props.canConnect} type="button">
                      <span className="btn-inner--icon">
                        {
                          reduxState.lidarr.isLoadingTags ? (
                            <Oval
                              wrapperClass="loader"
                              type="Oval"
                              color="#11cdef"
                              height={19}
                              width={19}
                            />)
                            : (<i className="fas fa-download"></i>)
                        }</span>
                      <span className="btn-inner--text">Load</span>
                    </button>
                  </div>
                  {
                    !reduxState.lidarr.areTagsValid ? (
                      <Alert className="mt-3 mb-4 text-wrap " color="warning">
                        <strong>Could not load tags, cannot reach Lidarr.</strong>
                      </Alert>)
                      : null
                  }
                </Col>
                <Col lg="6" className="mb-4">
                  <div className="input-group-button">
                    <Dropdown
                      name="Metadata Profile"
                      value={props.category.metadataProfileId}
                      items={reduxState.lidarr.metadataProfiles.map(x => { return { name: x.name, value: x.id } })}
                      onChange={newMetadataProfile => setCategory("metadataProfileId", newMetadataProfile)} />
                    <button className="btn btn-icon btn-3 btn-default" onClick={() => dispatch(loadMetadataProfiles(true))} disabled={!props.canConnect} type="button">
                      <span className="btn-inner--icon">
                        {
                          reduxState.lidarr.isLoadingMetadataProfiles ? (
                            <Oval
                              wrapperClass="loader"
                              type="Oval"
                              color="#11cdef"
                              height={19}
                              width={19}
                            />)
                            : (<i className="fas fa-download"></i>)
                        }</span>
                      <span className="btn-inner--text">Load</span>
                    </button>
                  </div>
                  {
                    !reduxState.lidarr.areMetadataProfilesValid ? (
                      <Alert className="mt-3 mb-0 text-wrap " color="warning">
                        <strong>Could not find any metadata profiles.</strong>
                      </Alert>)
                      : null
                  }
                  {
                    props.isSubmitted && reduxState.lidarr.metadataProfiles.length === 0 ? (
                      <Alert className="mt-3 mb-0 text-wrap " color="warning">
                        <strong>A metadata profile is required.</strong>
                      </Alert>)
                      : null
                  }
                </Col>
              </Row>
              {
                reduxState.lidarr.categories.length > 1
                  ? <Row>
                    <Col lg="12" className="text-right">
                      <button onClick={() => deleteCategory()} className="btn btn-icon btn-3 btn-danger" type="button">
                        <span className="btn-inner--icon"><i class="fas fa-trash"></i></span>
                        <span className="btn-inner--text">Remove</span>
                      </button>
                    </Col>
                  </Row>
                  : null
              }
            </div>
          </Collapse>
        </td>
      </tr>
    </>
  );
}

export default LidarrCategory;